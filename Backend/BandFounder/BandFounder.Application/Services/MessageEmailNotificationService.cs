using System.Text.Encodings.Web;
using BandFounder.Application.Services.Email;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BandFounder.Application.Services;

public interface IMessageEmailNotificationService
{
    Task<bool> ProcessDueAsync(CancellationToken cancellationToken = default);
}

public sealed class MessageEmailNotificationService : IMessageEmailNotificationService
{
    private static readonly string[] OutboxProcessIncludes =
    [
        nameof(EmailNotificationOutbox.RecipientAccount),
        $"{nameof(EmailNotificationOutbox.RecipientAccount)}.{nameof(Account.NotificationPreferences)}",
        nameof(EmailNotificationOutbox.Chatroom),
        $"{nameof(EmailNotificationOutbox.Chatroom)}.{nameof(Chatroom.Members)}"
    ];

    private readonly IEmailNotificationOutboxRepository _outboxRepository;
    private readonly IRepository<ChatroomReadState> _readStateRepository;
    private readonly IRepository<Message> _messageRepository;
    private readonly IEmailSender _emailSender;
    private readonly IMessageEmailNotificationGate _notificationGate;
    private readonly EmailOptions _emailOptions;
    private readonly MessageEmailNotificationOptions _notificationOptions;
    private readonly ILogger<MessageEmailNotificationService> _logger;

    public MessageEmailNotificationService(
        IEmailNotificationOutboxRepository outboxRepository,
        IRepository<ChatroomReadState> readStateRepository,
        IRepository<Message> messageRepository,
        IEmailSender emailSender,
        IMessageEmailNotificationGate notificationGate,
        IOptions<EmailOptions> emailOptions,
        IOptions<MessageEmailNotificationOptions> notificationOptions,
        ILogger<MessageEmailNotificationService> logger)
    {
        _outboxRepository = outboxRepository;
        _readStateRepository = readStateRepository;
        _messageRepository = messageRepository;
        _emailSender = emailSender;
        _notificationGate = notificationGate;
        _emailOptions = emailOptions.Value;
        _notificationOptions = notificationOptions.Value;
        _logger = logger;
    }

    public async Task<bool> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;

        var staleProcessing = (await _outboxRepository.GetAsync(outbox =>
                outbox.Status == EmailNotificationStatus.Processing &&
                outbox.LastAttemptAt.HasValue &&
                outbox.LastAttemptAt < now.AddMinutes(-_notificationOptions.StaleClaimMinutes)))
            .ToList();

        foreach (var stale in staleProcessing)
        {
            await _outboxRepository.TryRecoverStaleAsync(
                stale.Id,
                now.AddMinutes(-_notificationOptions.StaleClaimMinutes),
                now,
                _notificationOptions.MaxAttempts,
                cancellationToken);
        }

        var candidate = await _outboxRepository.GetOneAsync(
            outbox => outbox.Status == EmailNotificationStatus.Pending &&
                      outbox.NotBeforeUtc <= now);

        if (candidate is null)
        {
            return false;
        }

        var candidateId = candidate.Id;
        await _outboxRepository.DiscardAsync(candidate);

        if (!await _outboxRepository.TryClaimAsync(candidateId, now, cancellationToken))
        {
            return true;
        }

        var claimed = await _outboxRepository.GetOneRequiredAsync(
            outbox => outbox.Id == candidateId);
        var attemptCount = claimed.AttemptCount;
        await _outboxRepository.DiscardAsync(claimed);

        await _notificationGate.WaitBeforeSendEligibilityAsync(cancellationToken);

        // Re-load claim + membership/prefs from the database so concurrent leave/cancel/disable
        // cannot be missed by a stale in-memory snapshot from the original claim load.
        var due = await _outboxRepository.GetOneAsync(
            outbox => outbox.Id == candidateId &&
                      outbox.Status == EmailNotificationStatus.Processing &&
                      outbox.AttemptCount == attemptCount,
            OutboxProcessIncludes);

        if (due is null)
        {
            return true;
        }

        try
        {
            if (!due.Chatroom.Members.Any(member => member.Id == due.RecipientAccountId))
            {
                await _outboxRepository.TryMarkCancelledAsync(
                    due.Id,
                    due.AttemptCount,
                    DateTime.UtcNow,
                    cancellationToken);
                await _outboxRepository.DiscardAsync(due);
                return true;
            }

            if (!due.RecipientAccount.NotificationPreferences.EmailOnNewMessage ||
                !await HasUnreadMessageAsync(due))
            {
                await _outboxRepository.TryMarkCancelledAsync(
                    due.Id,
                    due.AttemptCount,
                    DateTime.UtcNow,
                    cancellationToken);
                await _outboxRepository.DiscardAsync(due);
                return true;
            }

            var email = BuildEmail(due);
            await _emailSender.SendAsync(email, cancellationToken);

            await _outboxRepository.TryMarkSentAsync(
                due.Id,
                due.AttemptCount,
                DateTime.UtcNow,
                cancellationToken);
            await _outboxRepository.DiscardAsync(due);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failedAt = DateTime.UtcNow;
            await _outboxRepository.TryTransitionAfterSendFailureAsync(
                due.Id,
                due.RecipientAccountId,
                due.ChatRoomId,
                due.CreatedAt,
                due.AttemptCount,
                _notificationOptions.MaxAttempts,
                failedAt.AddMinutes(5 * due.AttemptCount),
                failedAt,
                exception.Message,
                cancellationToken);
            await _outboxRepository.DiscardAsync(due);
            _logger.LogError(
                exception,
                "Failed to send message notification email for {ChatRoomId} to {RecipientAccountId}; attempt {AttemptCount}",
                due.ChatRoomId,
                due.RecipientAccountId,
                due.AttemptCount);
        }

        return true;
    }

    private async Task<bool> HasUnreadMessageAsync(EmailNotificationOutbox outbox)
    {
        var readState = await _readStateRepository.GetOneAsync(
            state => state.AccountId == outbox.RecipientAccountId &&
                     state.ChatRoomId == outbox.ChatRoomId);

        if (readState?.LastReadAt is DateTime lastReadAt)
        {
            return await _messageRepository.GetOneAsync(message =>
                message.ChatRoomId == outbox.ChatRoomId &&
                (message.SenderId == null || message.SenderId != outbox.RecipientAccountId) &&
                message.SentDate > lastReadAt) is not null;
        }

        return await _messageRepository.GetOneAsync(message =>
            message.ChatRoomId == outbox.ChatRoomId &&
            (message.SenderId == null || message.SenderId != outbox.RecipientAccountId)) is not null;
    }

    private OutgoingEmail BuildEmail(EmailNotificationOutbox outbox)
    {
        var senderName = CleanSubjectPart(outbox.LatestSenderName);
        var chatroomName = CleanSubjectPart(outbox.ChatroomName);
        var escapedSenderName = HtmlEncoder.Default.Encode(outbox.LatestSenderName);
        var escapedChatroomName = HtmlEncoder.Default.Encode(outbox.ChatroomName);
        var escapedSnippet = HtmlEncoder.Default.Encode(outbox.Snippet);
        var messagesUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/messages/{outbox.ChatRoomId}";

        return new OutgoingEmail
        {
            To = outbox.RecipientAccount.Email,
            Subject = $"New message from {senderName} in {chatroomName}",
            TextBody =
                $"{outbox.UnreadCountHint} unread message(s) from {outbox.LatestSenderName} in {outbox.ChatroomName}.\n\n" +
                $"{outbox.Snippet}\n\nOpen the conversation: {messagesUrl}",
            HtmlBody =
                $"<p>You have {outbox.UnreadCountHint} unread message(s) from {escapedSenderName} in {escapedChatroomName}.</p>" +
                $"<p>{escapedSnippet}</p>" +
                $"<p><a href=\"{HtmlEncoder.Default.Encode(messagesUrl)}\">Open the conversation</a></p>"
        };
    }

    private static string CleanSubjectPart(string value)
    {
        return value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }
}
