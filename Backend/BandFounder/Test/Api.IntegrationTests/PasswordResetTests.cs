using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[TestFixture]
public class PasswordResetTests : IntegrationTestBase
{
    [Test]
    public async Task RequestPasswordReset_UnknownEmail_ReturnsOkAndSendsNoEmail()
    {
        var response = await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "missing@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task RequestPasswordReset_KnownEmail_SendsResetLink()
    {
        await RegisterVerifiedAsync("resetuser", "resetuser@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        var response = await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "resetuser@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));

        var email = EmailSender.Sent[0];
        Assert.That(email.To, Is.EqualTo("resetuser@example.com"));
        Assert.That(email.Subject, Does.Contain("Reset"));
        Assert.That(email.TextBody, Does.Contain("http://127.0.0.1:3000/reset-password?token="));
    }

    [Test]
    public async Task CompletePasswordReset_WithValidToken_AllowsLoginWithNewPassword()
    {
        await RegisterVerifiedAsync("validreset", "validreset@example.com", "OldPassword123!");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "validreset@example.com"
        });

        var token = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var completeResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token,
            newPassword = "NewPassword123!"
        });

        Assert.That(completeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginWithOld = await Client.PostAsJsonAsync("/api/accounts/authenticate", new
        {
            usernameOrEmail = "validreset@example.com",
            password = "OldPassword123!"
        });
        Assert.That(loginWithOld.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var loginWithNew = await AuthenticateAsync("validreset@example.com", "NewPassword123!");
        Assert.That(loginWithNew, Does.Contain("."));
    }

    [Test]
    public async Task CompletePasswordReset_ReuseToken_ReturnsBadRequest()
    {
        await RegisterVerifiedAsync("reusereset", "reusereset@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "reusereset@example.com"
        });

        var token = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var first = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token,
            newPassword = "NewPassword123!"
        });
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var second = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token,
            newPassword = "AnotherPassword123!"
        });
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CompletePasswordReset_ExpiredToken_ReturnsBadRequest()
    {
        await RegisterVerifiedAsync("expiredreset", "expiredreset@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "expiredreset@example.com"
        });

        var token = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var resetToken = db.PasswordResetTokens.Single();
            resetToken.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token,
            newPassword = "NewPassword123!"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task RequestPasswordReset_InvalidatesPreviousActiveToken()
    {
        await RegisterVerifiedAsync("invalidate", "invalidate@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "invalidate@example.com"
        });
        var firstToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        EmailSender.ClearSent();

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "invalidate@example.com"
        });
        var secondToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        Assert.That(secondToken, Is.Not.EqualTo(firstToken));

        var oldTokenResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = firstToken,
            newPassword = "ShouldFail123!"
        });
        Assert.That(oldTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var newTokenResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = secondToken,
            newPassword = "ShouldSucceed123!"
        });
        Assert.That(newTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task RequestPasswordReset_EmailSendFails_ReturnsErrorAndAllowsRetry()
    {
        await RegisterVerifiedAsync("sendfail", "sendfail@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        EmailSender.ThrowOnSend = true;

        var response = await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "sendfail@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(EmailSender.Sent, Is.Empty);

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            Assert.That(db.PasswordResetTokens, Is.Empty);
        }

        EmailSender.ThrowOnSend = false;
        EmailSender.ClearSent();

        var retryResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "sendfail@example.com"
        });
        Assert.That(retryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));

        var workingToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var successfulReset = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = workingToken,
            newPassword = "RetryPassword123!"
        });
        Assert.That(successfulReset.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CompletePasswordReset_ConsumesSiblingActiveTokens()
    {
        await RegisterVerifiedAsync("siblingreset", "siblingreset@example.com", "OldPassword123!");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "siblingreset@example.com"
        });
        var firstToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        // Simulate a race: a second active token still present for the same account.
        string secondToken;
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            secondToken = SecureTokenHelper.GenerateRawToken();
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                AccountId = db.PasswordResetTokens.Single().AccountId,
                TokenHash = SecureTokenHelper.HashToken(secondToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });
            await db.SaveChangesAsync();
        }

        var completeResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = firstToken,
            newPassword = "NewPassword123!"
        });
        Assert.That(completeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var siblingResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = secondToken,
            newPassword = "AnotherPassword123!"
        });
        Assert.That(siblingResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var loginWithNew = await AuthenticateAsync("siblingreset@example.com", "NewPassword123!");
        Assert.That(loginWithNew, Does.Contain("."));
    }

    [Test]
    public async Task CompletePasswordReset_TrimsTokenWhitespace()
    {
        await RegisterVerifiedAsync("trimreset", "trimreset@example.com", "OldPassword123!");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "trimreset@example.com"
        });

        var token = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var completeResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = $"  {token}  ",
            newPassword = "NewPassword123!"
        });

        Assert.That(completeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginWithNew = await AuthenticateAsync("trimreset@example.com", "NewPassword123!");
        Assert.That(loginWithNew, Does.Contain("."));
    }

    [Test]
    public async Task CompletePasswordReset_InvalidatesExistingJwt()
    {
        var jwt = await RegisterVerifiedAsync("jwtrevoke", "jwtrevoke@example.com", "OldPassword123!");
        AuthenticateAs(jwt);

        var meBefore = await Client.GetAsync("/api/accounts/me");
        Assert.That(meBefore.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Client.DefaultRequestHeaders.Authorization = null;
        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "jwtrevoke@example.com"
        });
        var resetToken = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var completeResponse = await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token = resetToken,
            newPassword = "NewPassword123!"
        });
        Assert.That(completeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(jwt);
        var meAfter = await Client.GetAsync("/api/accounts/me");
        Assert.That(meAfter.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var newJwt = await AuthenticateAsync("jwtrevoke@example.com", "NewPassword123!");
        AuthenticateAs(newJwt);
        var meWithNewJwt = await Client.GetAsync("/api/accounts/me");
        Assert.That(meWithNewJwt.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task UpdateMyAccount_PasswordChange_InvalidatesExistingJwt()
    {
        var jwt = await RegisterVerifiedAsync("pwdupdate", "pwdupdate@example.com", "OldPassword123!");
        AuthenticateAs(jwt);

        var updateResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            password = "NewPassword123!"
        });
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var meAfter = await Client.GetAsync("/api/accounts/me");
        Assert.That(meAfter.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        Client.DefaultRequestHeaders.Authorization = null;
        var newJwt = await AuthenticateAsync("pwdupdate@example.com", "NewPassword123!");
        AuthenticateAs(newJwt);
        var meWithNewJwt = await Client.GetAsync("/api/accounts/me");
        Assert.That(meWithNewJwt.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetPasswordResetInfo_WithValidToken_ReturnsAccountDetails()
    {
        await RegisterVerifiedAsync("infouser", "infouser@example.com", "OldPassword123!");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "infouser@example.com"
        });
        var token = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        var response = await Client.GetAsync($"/api/accounts/password-reset/info?token={Uri.EscapeDataString(token)}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var info = await response.Content.ReadFromJsonAsync<PasswordResetTokenInfoDto>();
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Username, Is.EqualTo("infouser"));
        Assert.That(info.Email, Is.EqualTo("infouser@example.com"));
        Assert.That(info.HasProfilePicture, Is.False);
        Assert.That(info.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public async Task GetPasswordResetInfo_WithUnknownToken_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/accounts/password-reset/info?token=not-a-real-token");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetPasswordResetInfo_AfterTokenConsumed_ReturnsBadRequest()
    {
        await RegisterVerifiedAsync("consumedinfo", "consumedinfo@example.com", "OldPassword123!");
        Client.DefaultRequestHeaders.Authorization = null;

        await Client.PostAsJsonAsync("/api/accounts/password-reset/request", new
        {
            email = "consumedinfo@example.com"
        });
        var token = ExtractTokenFromEmail(EmailSender.Sent.Single().TextBody);

        await Client.PostAsJsonAsync("/api/accounts/password-reset/complete", new
        {
            token,
            newPassword = "NewPassword123!"
        });

        var response = await Client.GetAsync($"/api/accounts/password-reset/info?token={Uri.EscapeDataString(token)}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private static string ExtractTokenFromEmail(string textBody)
    {
        var match = Regex.Match(textBody, @"token=([^\s&]+)");
        Assert.That(match.Success, Is.True, "Reset token not found in email body");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}
