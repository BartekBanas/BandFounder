using BandFounder.Domain.Entities;

namespace BandFounder.Domain.Repositories;

public interface IEmailNotificationOutboxRepository : IRepository<EmailNotificationOutbox>
{
    Task AcquireQueueLockAsync(
        Guid recipientAccountId,
        Guid chatRoomId,
        CancellationToken cancellationToken = default);

    Task<bool> TryUpdatePendingAsync(
        Guid id,
        DateTime notBeforeUtc,
        string latestSenderName,
        string chatroomName,
        string snippet,
        int unreadCountHint,
        CancellationToken cancellationToken = default);

    Task CancelForRecipientChatroomAsync(
        Guid recipientAccountId,
        Guid chatRoomId,
        CancellationToken cancellationToken = default);

    Task DiscardAsync(EmailNotificationOutbox entity);

    Task<bool> TryClaimAsync(Guid id, DateTime now, CancellationToken cancellationToken = default);

    Task<bool> TryRecoverStaleAsync(
        Guid id,
        DateTime staleBeforeUtc,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<EmailNotificationStatus?> TryTransitionAfterSendFailureAsync(
        Guid id,
        Guid recipientAccountId,
        Guid chatRoomId,
        DateTime createdAtUtc,
        int attemptCount,
        int maxAttempts,
        DateTime retryAtUtc,
        DateTime processedAtUtc,
        string lastError,
        CancellationToken cancellationToken = default);
}
