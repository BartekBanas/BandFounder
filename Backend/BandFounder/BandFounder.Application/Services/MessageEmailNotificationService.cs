using System.Text.Encodings.Web;
using BandFounder.Application.Services.Email;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BandFounder.Application.Services;

public interface IMessageEmailNotificationService
{
    Task QueueAsync(Chatroom chatroom, Message message, Guid senderId);
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmailOptions _emailOptions;
    private readonly MessageEmailNotificationOptions _notificationOptions;
    private readonly ILogger<MessageEmailNotificationService> _logger;

    public MessageEmailNotificationService(
        IEmailNotificationOutboxRepository outboxRepository,
        IRepository<ChatroomReadState> readStateRepository,
        IRepository<Message> messageRepository,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IOptions<EmailOptions> emailOptions,
        IOptions<MessageEmailNotificationOptions> notificationOptions,
        ILogger<MessageEmailNotificationService> logger)
    {
        _outboxRepository = outboxRepository;
        _readStateRepository = readStateRepository;
        _messageRepository = messageRepository;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _emailOptions = emailOptions.Value;
        _notificationOptions = notificationOptions.Value;
        _logger = logger;
    }

    public async Task QueueAsync(Chatroom chatroom, Message message, Guid senderId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var senderName = chatroom.Members
                .FirstOrDefault(member => member.Id == senderId)?.Name ?? "Someone";
            var now = DateTime.UtcNow;

            foreach (var recipient in chatroom.Members.Where(member => member.Id != senderId))
            {
                if (!recipient.NotificationPreferences.EmailOnNewMessage)
                {
                    continue;
                }

                await _outboxRepository.AcquireQueueLockAsync(recipient.Id, chatroom.Id);

                var notBefore = now.AddMinutes(
                    GetDelayMinutes(recipient.NotificationPreferences.EmailUnreadDelayMinutes));
                var unreadCount = await GetUnreadMessageCountAsync(recipient.Id, chatroom.Id);
                var snippet = Truncate(message.Content);

                if (await TryRefreshPendingAsync(
                        recipient.Id,
                        chatroom.Id,
                        notBefore,
                        senderName,
                        chatroom.Name,
                        snippet,
                        unreadCount))
                {
                    continue;
                }

                var created = new EmailNotificationOutbox
                {
                    Id = Guid.NewGuid(),
                    RecipientAccountId = recipient.Id,
                    ChatRoomId = chatroom.Id,
                    Status = EmailNotificationStatus.Pending,
                    NotBeforeUtc = notBefore,
                    CreatedAt = now,
                    LatestSenderName = senderName,
                    ChatroomName = chatroom.Name,
                    Snippet = snippet,
                    UnreadCountHint = unreadCount
                };

                await _outboxRepository.CreateAsync(created);

                try
                {
                    await _outboxRepository.SaveChangesAsync();
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                {
                    await _outboxRepository.DiscardAsync(created);

                    if (!await TryRefreshPendingAsync(
                            recipient.Id,
                            chatroom.Id,
                            notBefore,
                            senderName,
                            chatroom.Name,
                            snippet,
                            unreadCount))
                    {
                        throw;
                    }
                }
            }
        });
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
            if (await _outboxRepository.TryRecoverStaleAsync(
                    stale.Id,
                    now.AddMinutes(-_notificationOptions.StaleClaimMinutes),
                    now,
                    cancellationToken))
            {
                stale.Status = EmailNotificationStatus.Pending;
                stale.NotBeforeUtc = now;
            }
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

        var due = await _outboxRepository.GetOneRequiredAsync(
            outbox => outbox.Id == candidateId,
            OutboxProcessIncludes);

        try
        {
            if (!due.Chatroom.Members.Any(member => member.Id == due.RecipientAccountId))
            {
                due.Status = EmailNotificationStatus.Cancelled;
                due.ProcessedAt = DateTime.UtcNow;
                await _outboxRepository.SaveChangesAsync();
                return true;
            }

            if (!due.RecipientAccount.NotificationPreferences.EmailOnNewMessage ||
                !await HasUnreadMessageAsync(due))
            {
                due.Status = EmailNotificationStatus.Cancelled;
                due.ProcessedAt = DateTime.UtcNow;
                await _outboxRepository.SaveChangesAsync();
                return true;
            }

            var email = BuildEmail(due);
            await _emailSender.SendAsync(email, cancellationToken);

            due.Status = EmailNotificationStatus.Sent;
            due.ProcessedAt = DateTime.UtcNow;
            due.LastError = null;
            await _outboxRepository.SaveChangesAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            due.Status = due.AttemptCount >= _notificationOptions.MaxAttempts
                ? EmailNotificationStatus.Failed
                : EmailNotificationStatus.Pending;
            due.NotBeforeUtc = DateTime.UtcNow.AddMinutes(5 * due.AttemptCount);
            due.LastError = exception.Message;
            if (due.Status == EmailNotificationStatus.Failed)
            {
                due.ProcessedAt = DateTime.UtcNow;
            }

            await _outboxRepository.SaveChangesAsync();
            _logger.LogError(
                exception,
                "Failed to send message notification email for {ChatRoomId} to {RecipientAccountId}; attempt {AttemptCount}",
                due.ChatRoomId,
                due.RecipientAccountId,
                due.AttemptCount);
        }

        return true;
    }

    private async Task<bool> TryRefreshPendingAsync(
        Guid recipientAccountId,
        Guid chatRoomId,
        DateTime notBeforeUtc,
        string latestSenderName,
        string chatroomName,
        string snippet,
        int unreadCountHint)
    {
        var candidates = (await _outboxRepository.GetAsync(outbox =>
            outbox.RecipientAccountId == recipientAccountId &&
            outbox.ChatRoomId == chatRoomId &&
            (outbox.Status == EmailNotificationStatus.Pending ||
             outbox.Status == EmailNotificationStatus.Processing))).ToList();
        var pending = candidates.FirstOrDefault(
            outbox => outbox.Status == EmailNotificationStatus.Pending);

        return pending is not null &&
               await _outboxRepository.TryUpdatePendingAsync(
                   pending.Id,
                   notBeforeUtc,
                   latestSenderName,
                   chatroomName,
                   snippet,
                   unreadCountHint);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("23505", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return exception.GetBaseException().Message.Contains("23505", StringComparison.Ordinal);
    }

    private async Task<int> GetUnreadMessageCountAsync(Guid recipientAccountId, Guid chatRoomId)
    {
        var readState = await _readStateRepository.GetOneAsync(
            state => state.AccountId == recipientAccountId &&
                     state.ChatRoomId == chatRoomId);

        if (readState?.LastReadAt is DateTime lastReadAt)
        {
            return await _messageRepository.CountAsync(message =>
                message.ChatRoomId == chatRoomId &&
                message.SenderId != recipientAccountId &&
                message.SentDate > lastReadAt);
        }

        return await _messageRepository.CountAsync(message =>
            message.ChatRoomId == chatRoomId &&
            message.SenderId != recipientAccountId);
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
                message.SenderId != outbox.RecipientAccountId &&
                message.SentDate > lastReadAt) is not null;
        }

        return await _messageRepository.GetOneAsync(message =>
            message.ChatRoomId == outbox.ChatRoomId &&
            message.SenderId != outbox.RecipientAccountId) is not null;
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

    private static int GetDelayMinutes(int configuredDelayMinutes)
    {
        return MessageEmailNotificationOptions.AllowedDelayMinutes.Contains(configuredDelayMinutes)
            ? configuredDelayMinutes
            : MessageEmailNotificationOptions.DefaultDelayMinutes;
    }

    private string Truncate(string content)
    {
        var normalized = content.Trim();
        return normalized.Length <= _notificationOptions.MaxSnippetLength
            ? normalized
            : $"{normalized[.._notificationOptions.MaxSnippetLength]}…";
    }

    private static string CleanSubjectPart(string value)
    {
        return value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }
}
