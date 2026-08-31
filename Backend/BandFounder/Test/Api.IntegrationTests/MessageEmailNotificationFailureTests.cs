using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[TestFixture]
public class MessageEmailNotificationFailureTests : IntegrationTestBase
{
    [Test]
    public async Task ProcessDueNotification_WhenProviderFailsWithSuccessorQueued_FailsOriginalAndSendsSuccessorOnce()
    {
        var (ownerToken, chatroom) = await CreateRoomWithMemberAsync("successorfailure");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "First message");
        await MakeOutboxDueAsync();

        var sendStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowSend = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        EmailSender.SendStarted = sendStarted;
        EmailSender.AllowSend = allowSend;
        EmailSender.ThrowOnSend = true;

        var firstProcessing = ProcessDueOnceAsync();
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Newer message");

        allowSend.SetResult(true);
        await firstProcessing;

        var afterFailure = await GetOutboxAsync();
        Assert.That(afterFailure, Has.Count.EqualTo(2));
        Assert.That(
            afterFailure.Single(outbox => outbox.Snippet == "First message").Status,
            Is.EqualTo(EmailNotificationStatus.Failed));
        Assert.That(
            afterFailure.Single(outbox => outbox.Snippet == "Newer message").Status,
            Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(EmailSender.Sent, Is.Empty);

        EmailSender.ThrowOnSend = false;
        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var finalOutbox = await GetOutboxAsync();
        Assert.That(
            finalOutbox.Single(outbox => outbox.Snippet == "First message").Status,
            Is.EqualTo(EmailNotificationStatus.Failed));
        Assert.That(
            finalOutbox.Single(outbox => outbox.Snippet == "Newer message").Status,
            Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent.Single().TextBody, Does.Contain("Newer message"));
    }

    [Test]
    public async Task StaleProcessingRow_WhenSuccessorIsProcessing_FailsOriginalWithoutResend()
    {
        var (ownerToken, chatroom) = await CreateRoomWithMemberAsync("staleprocessing");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "First message");
        await MakeOutboxDueAsync();

        var sendStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowSend = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        EmailSender.SendStarted = sendStarted;
        EmailSender.AllowSend = allowSend;

        var firstProcessing = ProcessDueOnceAsync();
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Newer message");

        allowSend.SetResult(true);
        await firstProcessing;

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var original = await dbContext.EmailNotificationOutboxes
                .SingleAsync(outbox => outbox.Snippet == "First message");
            original.Status = EmailNotificationStatus.Processing;
            original.AttemptCount = 1;
            original.LastAttemptAt = DateTime.UtcNow.AddMinutes(-16);
            original.ProcessedAt = null;

            var successor = await dbContext.EmailNotificationOutboxes
                .SingleAsync(outbox => outbox.Snippet == "Newer message");
            successor.Status = EmailNotificationStatus.Processing;
            successor.AttemptCount = 1;
            successor.LastAttemptAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        EmailSender.ClearSent();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(
            outbox.Single(row => row.Snippet == "First message").Status,
            Is.EqualTo(EmailNotificationStatus.Failed));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task StaleProcessingRow_WhenSuccessorAlreadySent_FailsOriginalWithoutResend()
    {
        var (ownerToken, chatroom) = await CreateRoomWithMemberAsync("stalesent");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "First message");
        await MakeOutboxDueAsync();

        var sendStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowSend = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        EmailSender.SendStarted = sendStarted;
        EmailSender.AllowSend = allowSend;

        var firstProcessing = ProcessDueOnceAsync();
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Newer message");

        allowSend.SetResult(true);
        await firstProcessing;
        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var original = await dbContext.EmailNotificationOutboxes
                .SingleAsync(outbox => outbox.Snippet == "First message");
            original.Status = EmailNotificationStatus.Processing;
            original.AttemptCount = 1;
            original.LastAttemptAt = DateTime.UtcNow.AddMinutes(-16);
            original.ProcessedAt = null;
            await dbContext.SaveChangesAsync();
        }

        EmailSender.ClearSent();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(
            outbox.Single(row => row.Snippet == "First message").Status,
            Is.EqualTo(EmailNotificationStatus.Failed));
        Assert.That(
            outbox.Single(row => row.Snippet == "Newer message").Status,
            Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task SendFailureTransition_WhenClaimWasReclaimed_DoesNotRewriteNewClaim()
    {
        var (ownerToken, chatroom) = await CreateRoomWithMemberAsync("reclaimedclaim");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Reclaimed notification");

        var original = (await GetOutboxAsync()).Single();
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var outbox = await dbContext.EmailNotificationOutboxes.SingleAsync();
            outbox.Status = EmailNotificationStatus.Processing;
            outbox.AttemptCount = 1;
            outbox.LastAttemptAt = DateTime.UtcNow.AddMinutes(-16);
            await dbContext.SaveChangesAsync();
        }

        var recoveryTime = DateTime.UtcNow;
        using (var scope = Services.CreateScope())
        {
            var repository = scope.ServiceProvider
                .GetRequiredService<IEmailNotificationOutboxRepository>();
            Assert.That(
                await repository.TryRecoverStaleAsync(
                    original.Id,
                    recoveryTime.AddMinutes(-15),
                    recoveryTime,
                    maxAttempts: 2),
                Is.True);
        }

        var reclaimedAttemptTime = DateTime.UtcNow;
        using (var scope = Services.CreateScope())
        {
            var repository = scope.ServiceProvider
                .GetRequiredService<IEmailNotificationOutboxRepository>();
            Assert.That(
                await repository.TryClaimAsync(original.Id, reclaimedAttemptTime),
                Is.True);
        }

        using (var scope = Services.CreateScope())
        {
            var repository = scope.ServiceProvider
                .GetRequiredService<IEmailNotificationOutboxRepository>();
            var result = await repository.TryTransitionAfterSendFailureAsync(
                original.Id,
                original.RecipientAccountId,
                original.ChatRoomId,
                original.CreatedAt,
                attemptCount: 1,
                maxAttempts: 2,
                reclaimedAttemptTime.AddMinutes(5),
                reclaimedAttemptTime,
                "stale worker failure");

            Assert.That(result, Is.Null);
        }

        var current = (await GetOutboxAsync()).Single();
        Assert.That(current.Status, Is.EqualTo(EmailNotificationStatus.Processing));
        Assert.That(current.AttemptCount, Is.EqualTo(2));
        Assert.That(current.LastError, Is.Null);
    }

    private async Task<(string OwnerToken, ChatroomDto Chatroom)> CreateRoomWithMemberAsync(
        string prefix)
    {
        var ownerToken = await RegisterVerifiedAsync($"{prefix}owner", $"{prefix}owner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = $"{prefix} room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var memberToken = await RegisterVerifiedAsync($"{prefix}member", $"{prefix}member@example.com");
        AuthenticateAs(memberToken);
        var memberResponse = await Client.GetAsync("/api/accounts/me");
        var member = await memberResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var memberId = member.GetProperty("id").GetString();

        AuthenticateAs(ownerToken);
        var inviteResponse = await Client.PostAsync(
            $"/api/chatrooms/{chatroom.Id}/invite/{memberId}", null);
        Assert.That(inviteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

        return (ownerToken, chatroom);
    }

    private async Task SendMessageAsync(Guid chatroomId, string content)
    {
        var sendContent = new StringContent(
            JsonSerializer.Serialize(content),
            Encoding.UTF8,
            "application/json");
        var response = await Client.PostAsync($"/api/chatrooms/{chatroomId}/messages", sendContent);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
    }

    private async Task<List<EmailNotificationOutbox>> GetOutboxAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        return await dbContext.EmailNotificationOutboxes.AsNoTracking().ToListAsync();
    }

    private async Task MakeOutboxDueAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var outbox = await dbContext.EmailNotificationOutboxes.ToListAsync();
        foreach (var notification in outbox)
        {
            notification.NotBeforeUtc = DateTime.UtcNow.AddMinutes(-1);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task ProcessDueAsync()
    {
        using var scope = Services.CreateScope();
        var notificationService = scope.ServiceProvider
            .GetRequiredService<IMessageEmailNotificationService>();
        while (await notificationService.ProcessDueAsync())
        {
        }
    }

    private async Task ProcessDueOnceAsync()
    {
        using var scope = Services.CreateScope();
        var notificationService = scope.ServiceProvider
            .GetRequiredService<IMessageEmailNotificationService>();
        await notificationService.ProcessDueAsync();
    }
}
