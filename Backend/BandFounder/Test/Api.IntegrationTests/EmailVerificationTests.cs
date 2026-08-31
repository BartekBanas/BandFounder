using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Email;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[TestFixture]
public class EmailVerificationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_SendsVerificationEmailAndLeavesAccountUnverified()
    {
        var token = await RegisterAsync("newuser", "newuser@example.com");

        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent[0].To, Is.EqualTo("newuser@example.com"));
        Assert.That(EmailSender.Sent[0].TextBody, Does.Contain("http://127.0.0.1:3000/verify-email?token="));

        AuthenticateAs(token);
        var me = await ReadJsonAsync<AccountSettingsDto>(await Client.GetAsync("/api/accounts/me"));
        Assert.Multiple(() =>
        {
            Assert.That(me.EmailVerified, Is.False);
            Assert.That(me.ResendAvailableAt, Is.GreaterThan(DateTime.UtcNow));
        });
    }

    [Test]
    public async Task ConfirmEmailVerification_WithValidToken_MarksAccountVerified()
    {
        var jwt = await RegisterAsync("verifyme", "verifyme@example.com");
        var rawToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var response = await Client.PostAsJsonAsync("/api/accounts/email-verification/confirm", new
        {
            token = rawToken
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(jwt);
        var me = await ReadJsonAsync<AccountSettingsDto>(await Client.GetAsync("/api/accounts/me"));
        Assert.That(me.EmailVerified, Is.True);
    }

    [Test]
    public async Task ConfirmEmailVerification_ExpiredToken_ReturnsBadRequest()
    {
        await RegisterAsync("expiredverify", "expiredverify@example.com");
        var rawToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var verificationToken = await db.EmailVerificationTokens.SingleAsync();
            verificationToken.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync("/api/accounts/email-verification/confirm", new
        {
            token = rawToken
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ConfirmEmailVerification_ReusedToken_ReturnsSuccessWhenAlreadyVerified()
    {
        await RegisterAsync("reverify", "reverify@example.com");
        var rawToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var first = await Client.PostAsJsonAsync("/api/accounts/email-verification/confirm", new {token = rawToken});
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var second = await Client.PostAsJsonAsync("/api/accounts/email-verification/confirm", new {token = rawToken});
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ResendEmailVerification_RespectsCooldown()
    {
        var jwt = await RegisterAsync("cooldown", "cooldown@example.com");
        AuthenticateAs(jwt);

        var tooSoon = await Client.PostAsync("/api/accounts/email-verification/resend", null);
        var cooldown = await ReadJsonAsync<EmailVerificationResendDto>(tooSoon);
        Assert.Multiple(() =>
        {
            Assert.That(tooSoon.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(tooSoon.Headers.RetryAfter?.Delta, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(cooldown.ResendAvailableAt, Is.GreaterThan(DateTime.UtcNow));
        });

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var verificationToken = await db.EmailVerificationTokens.SingleAsync();
            verificationToken.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
            await db.SaveChangesAsync();
        }

        EmailSender.ClearSent();
        var resend = await Client.PostAsync("/api/accounts/email-verification/resend", null);
        var result = await ReadJsonAsync<EmailVerificationResendDto>(resend);
        Assert.That(resend.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(result.ResendAvailableAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public async Task IssueEmailVerificationToken_ConcurrentAttempts_OnlyOneRotatesToken()
    {
        await RegisterAsync(
            "concurrentresend", "concurrentresend@example.com");

        Guid accountId;
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var account = await db.Accounts.SingleAsync(
                account => account.Name == "concurrentresend");
            accountId = account.Id;

            var initialToken = await db.EmailVerificationTokens.SingleAsync();
            initialToken.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
            await db.SaveChangesAsync();
        }

        var utcNow = DateTime.UtcNow;
        var attempts = Enumerable.Range(0, 8).Select(async _ =>
        {
            using var scope = Services.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IEmailVerificationStore>();
            var tokenId = Guid.NewGuid();
            return await store.IssueResendAsync(
                accountId,
                tokenId,
                tokenId.ToString("N"),
                utcNow,
                TimeSpan.FromDays(1),
                TimeSpan.FromMinutes(1));
        });

        var results = await Task.WhenAll(attempts);
        Assert.Multiple(() =>
        {
            Assert.That(
                results.Count(result =>
                    result.Status == EmailVerificationTokenIssuanceStatus.Issued),
                Is.EqualTo(1));
            Assert.That(
                results.Count(result =>
                    result.Status == EmailVerificationTokenIssuanceStatus.CooldownActive),
                Is.EqualTo(7));
        });

        using var verificationScope = Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var unconsumed = await verificationDb.EmailVerificationTokens
            .Where(token => token.AccountId == accountId && token.ConsumedAt == null)
            .ToListAsync();
        Assert.That(unconsumed, Has.Count.EqualTo(1));
        Assert.That(unconsumed[0].Id, Is.EqualTo(
            results.Single(result =>
                result.Status == EmailVerificationTokenIssuanceStatus.Issued).Token!.Id));
    }

    [Test]
    public async Task ResendEmailVerification_WhenSendFails_KeepsNewTokenQueued()
    {
        var jwt = await RegisterAsync(
            "resendfailure", "resendfailure@example.com");
        AuthenticateAs(jwt);

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var initialToken = await db.EmailVerificationTokens.SingleAsync();
            initialToken.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
            await db.SaveChangesAsync();
        }

        EmailSender.ThrowOnSend = true;
        var response = await Client.PostAsync("/api/accounts/email-verification/resend", null);
        var result = await ReadJsonAsync<EmailVerificationResendDto>(response);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result.ResendAvailableAt, Is.GreaterThan(DateTime.UtcNow));

        using var verificationScope = Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var queued = await verificationDb.EmailVerificationTokens
            .SingleAsync(token => token.ConsumedAt == null);
        Assert.Multiple(() =>
        {
            Assert.That(queued.DeliveryStatus, Is.EqualTo(EmailNotificationStatus.Pending));
            Assert.That(queued.DeliveryAttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateAccount_EmailChange_ClearsVerificationAndSendsNewLink()
    {
        var jwt = await RegisterVerifiedAsync("changeemail", "changeemail@example.com");
        AuthenticateAs(jwt);

        var response = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            email = "changeemail.updated@example.com"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await ReadJsonAsync<AccountSettingsDto>(response);
        Assert.That(updated.Email, Is.EqualTo("changeemail.updated@example.com"));
        Assert.That(updated.EmailVerified, Is.False);
        Assert.That(updated.ResendAvailableAt, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent[0].To, Is.EqualTo("changeemail.updated@example.com"));
    }

    [Test]
    public async Task UpdateAccount_EmailChange_OldTokenCannotVerifyNewEmail()
    {
        var jwt = await RegisterAsync("oldtoken", "oldtoken@example.com");
        var oldToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);
        AuthenticateAs(jwt);

        var updateResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            email = "oldtoken.updated@example.com"
        });
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var oldTokenResponse = await Client.PostAsJsonAsync(
            "/api/accounts/email-verification/confirm", new {token = oldToken});
        Assert.That(oldTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var settings = await ReadJsonAsync<AccountSettingsDto>(await Client.GetAsync("/api/accounts/me"));
        Assert.Multiple(() =>
        {
            Assert.That(settings.Email, Is.EqualTo("oldtoken.updated@example.com"));
            Assert.That(settings.EmailVerified, Is.False);
        });

        var newToken = ExtractTokenFromEmail(EmailSender.Sent.Last().TextBody);
        var newTokenResponse = await Client.PostAsJsonAsync(
            "/api/accounts/email-verification/confirm", new {token = newToken});
        Assert.That(newTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task UpdateAccount_WhenTokenInvalidationFails_RollsBackEmailAndVerificationReset()
    {
        var jwt = await RegisterVerifiedAsync("atomicemail", "atomicemail@example.com");
        AuthenticateAs(jwt);

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "EmailVerificationTokens"
                ADD CONSTRAINT "CK_EmailVerificationTokens_TestConsumedAtNull"
                CHECK ("ConsumedAt" IS NULL);
                """);
        }

        try
        {
            var response = await Client.PatchAsJsonAsync("/api/accounts/me", new
            {
                email = "atomicemail.updated@example.com"
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var account = await db.Accounts.SingleAsync(a => a.Name == "atomicemail");
            var verificationToken = await db.EmailVerificationTokens.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(account.Email, Is.EqualTo("atomicemail@example.com"));
                Assert.That(account.EmailVerifiedAt, Is.Not.Null);
                Assert.That(verificationToken.ConsumedAt, Is.Null);
                Assert.That(EmailSender.Sent, Is.Empty);
            });
        }
        finally
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "EmailVerificationTokens"
                DROP CONSTRAINT IF EXISTS "CK_EmailVerificationTokens_TestConsumedAtNull";
                """);
        }
    }

    [Test]
    public async Task Register_WhenSendFails_KeepsTokenQueuedForRetry()
    {
        EmailSender.ThrowOnSend = true;

        var jwt = await RegisterAsync("sendfailverify", "sendfailverify@example.com");
        Assert.That(jwt, Does.Contain("."));
        Assert.That(EmailSender.Sent, Is.Empty);

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var queued = await db.EmailVerificationTokens.SingleAsync();
            Assert.Multiple(() =>
            {
                Assert.That(queued.DeliveryStatus, Is.EqualTo(EmailNotificationStatus.Pending));
                Assert.That(queued.DeliveryAttemptCount, Is.EqualTo(1));
                Assert.That(queued.ConsumedAt, Is.Null);
            });
            queued.DeliveryNotBeforeUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        EmailSender.ThrowOnSend = false;
        using (var scope = Services.CreateScope())
        {
            var verificationService = scope.ServiceProvider
                .GetRequiredService<IEmailVerificationService>();
            Assert.That(await verificationService.ProcessDueAsync(), Is.True);
        }

        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            Assert.That(
                (await db.EmailVerificationTokens.SingleAsync()).DeliveryStatus,
                Is.EqualTo(EmailNotificationStatus.Sent));
        }
    }

    [Test]
    public async Task Register_WhenProviderOutcomeIsAmbiguous_DeliveredLinkRemainsValid()
    {
        EmailSender.ThrowOnSend = true;
        EmailSender.ThrowAfterRecording = true;

        await RegisterAsync(
            "ambiguousverify",
            "ambiguousverify@example.com");
        var rawToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var response = await Client.PostAsJsonAsync(
            "/api/accounts/email-verification/confirm",
            new {token = rawToken});

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var token = await db.EmailVerificationTokens.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(token.ConsumedAt, Is.Not.Null);
            Assert.That(token.DeliveryStatus, Is.EqualTo(EmailNotificationStatus.Cancelled));
        });
    }

    [Test]
    public async Task RegisterVerified_PreservesUnrelatedEmailAndSenderConfiguration()
    {
        var unrelatedEmail = new OutgoingEmail
        {
            To = "existing@example.com",
            Subject = "Existing evidence",
            TextBody = "Existing text",
            HtmlBody = "<p>Existing HTML</p>"
        };
        await EmailSender.SendAsync(unrelatedEmail);

        var sendStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowSend = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        allowSend.SetResult(true);
        EmailSender.SendStarted = sendStarted;
        EmailSender.AllowSend = allowSend;
        EmailSender.ThrowOnSend = true;
        EmailSender.ThrowAfterRecording = true;

        await RegisterVerifiedAsync("fixtureverified", "fixtureverified@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(EmailSender.Sent, Is.EqualTo(new[] { unrelatedEmail }));
            Assert.That(EmailSender.ThrowOnSend, Is.True);
            Assert.That(EmailSender.ThrowAfterRecording, Is.True);
            Assert.That(EmailSender.SendStarted, Is.SameAs(sendStarted));
            Assert.That(EmailSender.AllowSend, Is.SameAs(allowSend));
        });

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var account = await db.Accounts.SingleAsync(
            account => account.Email == "fixtureverified@example.com");
        var verificationToken = await db.EmailVerificationTokens.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(account.EmailVerifiedAt, Is.Not.Null);
            Assert.That(verificationToken.ConsumedAt, Is.Not.Null);
            Assert.That(
                verificationToken.DeliveryStatus,
                Is.EqualTo(EmailNotificationStatus.Cancelled));
        });
    }

    private static string ExtractTokenFromEmail(string textBody)
    {
        var match = Regex.Match(textBody, @"token=([^\s&]+)");
        Assert.That(match.Success, Is.True, "Verification token not found in email body");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}
