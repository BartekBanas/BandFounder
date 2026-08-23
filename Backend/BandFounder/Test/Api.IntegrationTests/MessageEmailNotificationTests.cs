using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Api.BackgroundServices;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Email;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Api.IntegrationTests;

[TestFixture]
public class MessageEmailNotificationTests : IntegrationTestBase
{
    [Test]
    public async Task SendMessage_QueuesOneNotificationForEachRecipientAndNotSender()
    {
        var (ownerToken, chatroom, _, member) = await CreateRoomWithMemberAsync("queue");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "A message for the member");

        var outbox = await GetOutboxAsync();

        Assert.That(outbox, Has.Count.EqualTo(1));
        Assert.That(outbox[0].RecipientAccountId, Is.EqualTo(Guid.Parse(member.Id)));
        Assert.That(outbox[0].ChatRoomId, Is.EqualTo(chatroom.Id));
        Assert.That(outbox[0].Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(outbox[0].LatestSenderName, Is.EqualTo("queueowner"));
        Assert.That(member.Email, Is.EqualTo("queuemember@example.com"));
    }

    [Test]
    public async Task SendMessage_WithEmailPreferenceDisabled_DoesNotQueueNotification()
    {
        var (ownerToken, chatroom, memberToken, _) = await CreateRoomWithMemberAsync("disabled");

        AuthenticateAs(memberToken);
        var updateResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            emailOnNewMessage = false
        });
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "This should not create email work");

        Assert.That(await GetOutboxAsync(), Is.Empty);
    }

    [Test]
    public async Task ProcessDueNotification_AfterRecipientReadsConversation_CancelsWithoutSending()
    {
        var (ownerToken, chatroom, memberToken, _) = await CreateRoomWithMemberAsync("cancel");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Read this in the site");

        AuthenticateAs(memberToken);
        var markReadResponse = await Client.PutAsync($"/api/chatrooms/{chatroom.Id}/read", null);
        Assert.That(markReadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(outbox.Single().Status, Is.EqualTo(EmailNotificationStatus.Cancelled));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task DeletedSender_UnreadMessage_StillDeliversNotification()
    {
        var (ownerToken, chatroom, _, member) = await CreateRoomWithMemberAsync("deletesender");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Message before sender deletes");

        var queued = (await GetOutboxAsync()).Single();
        Assert.That(queued.Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(queued.UnreadCountHint, Is.EqualTo(1));
        Assert.That(queued.RecipientAccountId, Is.EqualTo(Guid.Parse(member.Id)));

        AuthenticateAs(ownerToken);
        var deleteResponse = await Client.DeleteAsync("/api/accounts/me");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var message = await dbContext.Messages.SingleAsync(m => m.ChatRoomId == chatroom.Id);
            Assert.That(message.SenderId, Is.Null);
        }

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(outbox.Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent[0].To, Is.EqualTo(member.Email));
        Assert.That(EmailSender.Sent[0].TextBody, Does.Contain("1 unread message(s)"));
    }

    [Test]
    public async Task DeletedSender_UnreadMessage_IsIncludedInUnreadCountHintOnUpdate()
    {
        var (ownerToken, chatroom, memberToken, member) = await CreateRoomWithMemberAsync("deletecount");

        AuthenticateAs(memberToken);
        var markReadResponse = await Client.PutAsync($"/api/chatrooms/{chatroom.Id}/read", null);
        Assert.That(markReadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "From sender who will delete");

        var afterFirst = (await GetOutboxAsync()).Single();
        Assert.That(afterFirst.RecipientAccountId, Is.EqualTo(Guid.Parse(member.Id)));
        Assert.That(afterFirst.UnreadCountHint, Is.EqualTo(1));

        AuthenticateAs(ownerToken);
        var deleteResponse = await Client.DeleteAsync("/api/accounts/me");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var orphaned = await dbContext.Messages.SingleAsync(
                m => m.ChatRoomId == chatroom.Id && m.Content == "From sender who will delete");
            Assert.That(orphaned.SenderId, Is.Null);
        }

        var thirdToken = await RegisterAsync("deletecountthird", "deletecountthird@example.com");
        AuthenticateAs(thirdToken);
        var third = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        AuthenticateAs(memberToken);
        var inviteResponse = await Client.PostAsync(
            $"/api/chatrooms/{chatroom.Id}/invite/{third.Id}", null);
        Assert.That(inviteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(thirdToken);
        await SendMessageAsync(chatroom.Id, "From remaining member");

        var updated = (await GetOutboxAsync())
            .Single(row => row.RecipientAccountId == Guid.Parse(member.Id));
        Assert.That(updated.Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(updated.UnreadCountHint, Is.EqualTo(2));
        Assert.That(updated.Snippet, Is.EqualTo("From remaining member"));

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        Assert.That(
            (await GetOutboxAsync()).Single(row => row.RecipientAccountId == Guid.Parse(member.Id)).Status,
            Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent.Any(email => email.To == member.Email), Is.True);
    }

    [Test]
    public async Task ProcessDueNotification_WhenStillUnread_SendsOneSafeEmail()
    {
        var (ownerToken, chatroom, _, member) = await CreateRoomWithMemberAsync("send");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "<script>alert('private')</script>");

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(outbox.Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent[0].To, Is.EqualTo(member.Email));
        Assert.That(EmailSender.Sent[0].TextBody, Does.Contain($"/messages/{chatroom.Id}"));
        Assert.That(EmailSender.Sent[0].HtmlBody, Does.Contain("&lt;script&gt;"));
        Assert.That(EmailSender.Sent[0].HtmlBody, Does.Not.Contain("<script>"));

        await ProcessDueAsync();
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SendMessageTwice_BeforeDelivery_DeduplicatesPendingWork()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("dedupe");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "First message");
        await SendMessageAsync(chatroom.Id, "Second message");

        var outbox = await GetOutboxAsync();

        Assert.That(outbox, Has.Count.EqualTo(1));
        Assert.That(outbox[0].UnreadCountHint, Is.EqualTo(2));
        Assert.That(outbox[0].Snippet, Is.EqualTo("Second message"));
        Assert.That(outbox[0].Status, Is.EqualTo(EmailNotificationStatus.Pending));
    }

    [Test]
    public async Task FiveUnreadMessages_GroupIntoOneEmailWithLatestSnippet()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("group");

        AuthenticateAs(ownerToken);
        for (var index = 1; index <= 5; index++)
        {
            await SendMessageAsync(chatroom.Id, $"Message {index}");
        }

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(outbox, Has.Count.EqualTo(1));
        Assert.That(outbox.Single().UnreadCountHint, Is.EqualTo(5));
        Assert.That(outbox.Single().Snippet, Is.EqualTo("Message 5"));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent.Single().TextBody, Does.Contain("5 unread message(s)"));
        Assert.That(EmailSender.Sent.Single().TextBody, Does.Contain("Message 5"));
    }

    [Test]
    public async Task ReadingBeforeAnotherMessage_OnlyCountsTheNewUnreadMessage()
    {
        var (ownerToken, chatroom, memberToken, _) = await CreateRoomWithMemberAsync("readstate");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Read this first");

        AuthenticateAs(memberToken);
        var markReadResponse = await Client.PutAsync($"/api/chatrooms/{chatroom.Id}/read", null);
        Assert.That(markReadResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Only this one is unread");

        var outbox = await GetOutboxAsync();
        Assert.That(outbox, Has.Count.EqualTo(1));
        Assert.That(outbox.Single().UnreadCountHint, Is.EqualTo(1));
        Assert.That(outbox.Single().Snippet, Is.EqualTo("Only this one is unread"));
    }

    [Test]
    public async Task ProcessDueNotification_WhenProviderFails_RetriesAndEventuallySends()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("retry");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Retry this notification");
        await MakeOutboxDueAsync();

        EmailSender.ThrowOnSend = true;
        await ProcessDueAsync();

        var failedAttempt = (await GetOutboxAsync()).Single();
        Assert.That(failedAttempt.Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(failedAttempt.AttemptCount, Is.EqualTo(1));
        Assert.That(EmailSender.Sent, Is.Empty);

        EmailSender.ThrowOnSend = false;
        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        Assert.That((await GetOutboxAsync()).Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ProcessDueNotification_WhenProviderFailsAfterEarlierNotificationWasSent_Retries()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("retryafterprevious");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Earlier notification");
        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Retry this notification");
        await MakeOutboxDueAsync();

        EmailSender.ThrowOnSend = true;
        await ProcessDueAsync();

        var failedAttempt = (await GetOutboxAsync())
            .Single(outbox => outbox.Snippet == "Retry this notification");
        Assert.That(failedAttempt.Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(failedAttempt.AttemptCount, Is.EqualTo(1));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));

        EmailSender.ThrowOnSend = false;
        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        Assert.That(
            (await GetOutboxAsync()).Single(outbox => outbox.Snippet == "Retry this notification").Status,
            Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ProcessDueNotification_WhenProviderKeepsFailing_ReachesConfiguredFailureState()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("failed");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "This will fail");
        await MakeOutboxDueAsync();

        EmailSender.ThrowOnSend = true;
        await ProcessDueAsync();
        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var outbox = (await GetOutboxAsync()).Single();
        Assert.That(outbox.Status, Is.EqualTo(EmailNotificationStatus.Failed));
        Assert.That(outbox.AttemptCount, Is.EqualTo(2));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task LeaveChatroom_CancelsPendingNotificationWithoutSending()
    {
        var (ownerToken, chatroom, memberToken, _) = await CreateRoomWithMemberAsync("leave");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "You will leave before this emails");

        AuthenticateAs(memberToken);
        var leaveResponse = await Client.PostAsync($"/api/chatrooms/{chatroom.Id}/leave", null);
        Assert.That(leaveResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        var outbox = await GetOutboxAsync();
        Assert.That(outbox.Single().Status, Is.EqualTo(EmailNotificationStatus.Cancelled));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task LeaveChatroom_WhileClaimed_DoesNotSendEmail()
    {
        var (ownerToken, chatroom, memberToken, _) = await CreateRoomWithMemberAsync("leavewhileclaimed");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Leave before this is emailed");
        await MakeOutboxDueAsync();

        var eligibilityStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowEligibility = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        NotificationGate.EligibilityStarted = eligibilityStarted;
        NotificationGate.AllowEligibility = allowEligibility;

        var processing = ProcessDueOnceAsync();
        await eligibilityStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        AuthenticateAs(memberToken);
        var leaveResponse = await Client.PostAsync($"/api/chatrooms/{chatroom.Id}/leave", null);
        Assert.That(leaveResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        allowEligibility.SetResult(true);
        await processing;

        var outbox = await GetOutboxAsync();
        Assert.That(outbox.Single().Status, Is.EqualTo(EmailNotificationStatus.Cancelled));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task SendMessage_PersistsMessageAndPendingNotificationIntentThatCanLaterBeDelivered()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("durable");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Message with durable notification intent");

        var queuedIntent = (await GetOutboxAsync()).Single();
        Assert.That(queuedIntent.Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(queuedIntent.ChatRoomId, Is.EqualTo(chatroom.Id));
        Assert.That(queuedIntent.Snippet, Is.EqualTo("Message with durable notification intent"));

        var messagesResponse = await Client.GetAsync($"/api/chatrooms/{chatroom.Id}/messages");
        Assert.That(messagesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var messages = await ReadJsonAsync<List<JsonElement>>(messagesResponse);
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(
            messages[0].GetProperty("content").GetString(),
            Is.EqualTo("Message with durable notification intent"));

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        Assert.That((await GetOutboxAsync()).Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SendMessageAcrossMultipleMessages_PreservesUnreadCountHint()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("durablecount");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Unread one");
        await SendMessageAsync(chatroom.Id, "Unread two");
        await SendMessageAsync(chatroom.Id, "Unread three");

        var queuedIntent = (await GetOutboxAsync()).Single();
        Assert.That(queuedIntent.Status, Is.EqualTo(EmailNotificationStatus.Pending));
        Assert.That(queuedIntent.UnreadCountHint, Is.EqualTo(3));
        Assert.That(queuedIntent.Snippet, Is.EqualTo("Unread three"));

        await MakeOutboxDueAsync();
        await ProcessDueAsync();

        Assert.That((await GetOutboxAsync()).Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That(EmailSender.Sent.Single().TextBody, Does.Contain("3 unread message(s)"));
    }

    [Test]
    public void MessageNotificationOptions_AreBoundAndValidatedConfigurationIsAvailable()
    {
        var options = Services.GetRequiredService<IOptions<MessageEmailNotificationOptions>>().Value;

        Assert.That(options.PollIntervalSeconds, Is.EqualTo(1));
        Assert.That(options.MaxAttempts, Is.EqualTo(2));
        Assert.That(options.MaxSnippetLength, Is.EqualTo(160));
        Assert.That(options.StaleClaimMinutes, Is.EqualTo(15));
    }

    [Test]
    public async Task NotificationDelayAndSnippetLength_UseConfiguredValues()
    {
        var (ownerToken, chatroom, memberToken, _) = await CreateRoomWithMemberAsync("config");
        AuthenticateAs(memberToken);
        var preferenceResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            emailUnreadDelayMinutes = 5
        });
        Assert.That(preferenceResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthenticateAs(ownerToken);
        var beforeSend = DateTime.UtcNow;
        await SendMessageAsync(chatroom.Id, new string('x', 161));

        var outbox = (await GetOutboxAsync()).Single();
        Assert.That(outbox.NotBeforeUtc, Is.GreaterThan(beforeSend.AddMinutes(4)));
        Assert.That(outbox.Snippet.Length, Is.EqualTo(161));
        Assert.That(outbox.Snippet.EndsWith('…'), Is.True);
        Assert.That(outbox.Snippet[..160], Is.EqualTo(new string('x', 160)));
    }

    [Test]
    public async Task StaleProcessingRow_IsRecoveredAndDelivered()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("stale");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Recover this notification");

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var outbox = await dbContext.EmailNotificationOutboxes.SingleAsync();
            outbox.Status = EmailNotificationStatus.Processing;
            outbox.AttemptCount = 1;
            outbox.LastAttemptAt = DateTime.UtcNow.AddMinutes(-16);
            await dbContext.SaveChangesAsync();
        }

        await ProcessDueAsync();

        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That((await GetOutboxAsync()).Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
    }

    [Test]
    public async Task StaleProcessingRow_AtMaxAttempts_IsFailedWithoutSending()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("stalemax");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Do not resend this notification");

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
            var outbox = await dbContext.EmailNotificationOutboxes.SingleAsync();
            outbox.Status = EmailNotificationStatus.Processing;
            outbox.AttemptCount = 2;
            outbox.LastAttemptAt = DateTime.UtcNow.AddMinutes(-16);
            await dbContext.SaveChangesAsync();
        }

        await ProcessDueAsync();

        var recovered = (await GetOutboxAsync()).Single();
        Assert.That(recovered.Status, Is.EqualTo(EmailNotificationStatus.Failed));
        Assert.That(recovered.AttemptCount, Is.EqualTo(2));
        Assert.That(EmailSender.Sent, Is.Empty);
    }

    [Test]
    public async Task ConcurrentProcessing_OnlyOneWorkerClaimsAndSendsTheRow()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("claim");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "Claim this once");
        await MakeOutboxDueAsync();

        await Task.WhenAll(ProcessDueOnceAsync(), ProcessDueOnceAsync());

        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        Assert.That((await GetOutboxAsync()).Single().Status, Is.EqualTo(EmailNotificationStatus.Sent));
    }

    [Test]
    public async Task WorkerCycle_TwoDueRoomsForSameRecipient_HonorsMidCycleEmailOptOut()
    {
        var ownerToken = await RegisterAsync("scopesowner", "scopesowner@example.com");
        AuthenticateAs(ownerToken);

        var room1 = await ReadJsonAsync<ChatroomDto>(await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "scopes room 1"
        }));
        var room2 = await ReadJsonAsync<ChatroomDto>(await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = "scopes room 2"
        }));

        var memberToken = await RegisterAsync("scopesmember", "scopesmember@example.com");
        AuthenticateAs(memberToken);
        var member = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        AuthenticateAs(ownerToken);
        Assert.That(
            (await Client.PostAsync($"/api/chatrooms/{room1.Id}/invite/{member.Id}", null)).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
        Assert.That(
            (await Client.PostAsync($"/api/chatrooms/{room2.Id}/invite/{member.Id}", null)).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        await SendMessageAsync(room1.Id, "First room due notification");
        await SendMessageAsync(room2.Id, "Second room due notification");
        await MakeOutboxDueAsync();
        Assert.That(await GetOutboxAsync(), Has.Count.EqualTo(2));

        var sendStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowSend = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        EmailSender.SendStarted = sendStarted;
        EmailSender.AllowSend = allowSend;

        var worker = ActivatorUtilities.CreateInstance<MessageEmailNotificationWorker>(Services);
        var cycleTask = worker.ProcessDueNotificationsAsync(CancellationToken.None);

        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        AuthenticateAs(memberToken);
        var optOutResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            emailOnNewMessage = false
        });
        Assert.That(optOutResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        allowSend.SetResult(true);
        await cycleTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(EmailSender.Sent, Has.Count.EqualTo(1));
        var outbox = await GetOutboxAsync();
        Assert.That(outbox.Count(row => row.Status == EmailNotificationStatus.Sent), Is.EqualTo(1));
        Assert.That(outbox.Count(row => row.Status == EmailNotificationStatus.Cancelled), Is.EqualTo(1));
    }

    [Test]
    public async Task EnqueueWhileSending_CreatesSuccessorWorkWithoutLosingNewMessage()
    {
        var (ownerToken, chatroom, _, _) = await CreateRoomWithMemberAsync("successor");

        AuthenticateAs(ownerToken);
        await SendMessageAsync(chatroom.Id, "First message");
        await MakeOutboxDueAsync();

        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        Assert.That(EmailSender.Sent, Has.Count.EqualTo(2));
        Assert.That(EmailSender.Sent.Any(email => email.TextBody.Contains("Newer message")), Is.True);
        Assert.That((await GetOutboxAsync()).Count(outbox => outbox.Status == EmailNotificationStatus.Sent), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateAccount_EmailPreferencesAreReturnedAndDelayIsValidated()
    {
        var token = await RegisterAsync("settingsuser", "settingsuser@example.com");
        AuthenticateAs(token);

        var updateResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            emailOnNewMessage = false,
            emailUnreadDelayMinutes = 60
        });

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await ReadJsonAsync<AccountSettingsDto>(updateResponse);
        Assert.That(updated.EmailOnNewMessage, Is.False);
        Assert.That(updated.EmailUnreadDelayMinutes, Is.EqualTo(60));

        var invalidResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            emailUnreadDelayMinutes = 30
        });

        Assert.That(invalidResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<(string OwnerToken, ChatroomDto Chatroom, string MemberToken, AccountDto Member)>
        CreateRoomWithMemberAsync(string prefix)
    {
        var ownerToken = await RegisterAsync($"{prefix}owner", $"{prefix}owner@example.com");
        AuthenticateAs(ownerToken);

        var createResponse = await Client.PostAsJsonAsync("/api/chatrooms", new
        {
            chatRoomType = ChatRoomType.General,
            name = $"{prefix} room"
        });
        var chatroom = await ReadJsonAsync<ChatroomDto>(createResponse);

        var memberToken = await RegisterAsync($"{prefix}member", $"{prefix}member@example.com");
        AuthenticateAs(memberToken);
        var member = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        AuthenticateAs(ownerToken);
        var inviteResponse = await Client.PostAsync(
            $"/api/chatrooms/{chatroom.Id}/invite/{member.Id}", null);
        Assert.That(inviteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        return (ownerToken, chatroom, memberToken, member);
    }

    private async Task SendMessageAsync(Guid chatroomId, string content)
    {
        var sendContent = new StringContent(
            JsonSerializer.Serialize(content),
            Encoding.UTF8,
            "application/json");
        var response = await Client.PostAsync($"/api/chatrooms/{chatroomId}/messages", sendContent);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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
