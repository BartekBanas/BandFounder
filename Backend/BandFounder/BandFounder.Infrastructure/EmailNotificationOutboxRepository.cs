using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public sealed class EmailNotificationOutboxRepository
    : Repository<EmailNotificationOutbox, BandFounderDbContext>, IEmailNotificationOutboxRepository
{
    private readonly BandFounderDbContext _dbContext;

    public EmailNotificationOutboxRepository(BandFounderDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AcquireQueueLockAsync(
        Guid recipientAccountId,
        Guid chatRoomId,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"{recipientAccountId:D}:{chatRoomId:D}";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    public async Task<bool> TryUpdatePendingAsync(
        Guid id,
        DateTime notBeforeUtc,
        string latestSenderName,
        string chatroomName,
        string snippet,
        int unreadCountHint,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.EmailNotificationOutboxes
            .Where(outbox =>
                outbox.Id == id &&
                outbox.Status == EmailNotificationStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.NotBeforeUtc, notBeforeUtc)
                    .SetProperty(outbox => outbox.LatestSenderName, latestSenderName)
                    .SetProperty(outbox => outbox.ChatroomName, chatroomName)
                    .SetProperty(outbox => outbox.Snippet, snippet)
                    .SetProperty(outbox => outbox.UnreadCountHint, unreadCountHint)
                    .SetProperty(outbox => outbox.LastError, (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task CancelForRecipientChatroomAsync(
        Guid recipientAccountId,
        Guid chatRoomId,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.EmailNotificationOutboxes
            .Where(outbox =>
                outbox.RecipientAccountId == recipientAccountId &&
                outbox.ChatRoomId == chatRoomId &&
                (outbox.Status == EmailNotificationStatus.Pending ||
                 outbox.Status == EmailNotificationStatus.Processing))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, EmailNotificationStatus.Cancelled)
                    .SetProperty(outbox => outbox.ProcessedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public Task DiscardAsync(EmailNotificationOutbox entity)
    {
        var entry = _dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }

        return Task.CompletedTask;
    }

    public async Task<bool> TryClaimAsync(
        Guid id,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.EmailNotificationOutboxes
            .Where(outbox =>
                outbox.Id == id &&
                outbox.Status == EmailNotificationStatus.Pending &&
                outbox.NotBeforeUtc <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, EmailNotificationStatus.Processing)
                    .SetProperty(outbox => outbox.AttemptCount, outbox => outbox.AttemptCount + 1)
                    .SetProperty(outbox => outbox.LastAttemptAt, now),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryMarkSentAsync(
        Guid id,
        int attemptCount,
        DateTime processedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.EmailNotificationOutboxes
            .Where(outbox =>
                outbox.Id == id &&
                outbox.Status == EmailNotificationStatus.Processing &&
                outbox.AttemptCount == attemptCount)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, EmailNotificationStatus.Sent)
                    .SetProperty(outbox => outbox.ProcessedAt, processedAt)
                    .SetProperty(outbox => outbox.LastError, (string?)null),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryMarkCancelledAsync(
        Guid id,
        int attemptCount,
        DateTime processedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.EmailNotificationOutboxes
            .Where(outbox =>
                outbox.Id == id &&
                outbox.Status == EmailNotificationStatus.Processing &&
                outbox.AttemptCount == attemptCount)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, EmailNotificationStatus.Cancelled)
                    .SetProperty(outbox => outbox.ProcessedAt, processedAt),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryRecoverStaleAsync(
        Guid id,
        DateTime staleBeforeUtc,
        DateTime now,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        var stale = await _dbContext.EmailNotificationOutboxes
            .AsNoTracking()
            .Where(outbox =>
                outbox.Id == id &&
                outbox.Status == EmailNotificationStatus.Processing &&
                outbox.LastAttemptAt < staleBeforeUtc)
            .Select(outbox => new
            {
                outbox.RecipientAccountId,
                outbox.ChatRoomId,
                outbox.CreatedAt,
                outbox.AttemptCount
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (stale is null)
        {
            return false;
        }

        return await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);
            await AcquireQueueLockAsync(
                stale.RecipientAccountId,
                stale.ChatRoomId,
                cancellationToken);

            var hasSupersedingSuccessor = await HasSupersedingSuccessorAsync(
                id,
                stale.RecipientAccountId,
                stale.ChatRoomId,
                stale.CreatedAt,
                cancellationToken);
            var recoveredStatus = hasSupersedingSuccessor
                || stale.AttemptCount >= maxAttempts
                ? EmailNotificationStatus.Failed
                : EmailNotificationStatus.Pending;

            var affectedRows = await _dbContext.EmailNotificationOutboxes
                .Where(outbox =>
                    outbox.Id == id &&
                    outbox.Status == EmailNotificationStatus.Processing &&
                    outbox.LastAttemptAt < staleBeforeUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(outbox => outbox.Status, recoveredStatus)
                        .SetProperty(outbox => outbox.NotBeforeUtc, now)
                        .SetProperty(
                            outbox => outbox.ProcessedAt,
                            recoveredStatus == EmailNotificationStatus.Failed ? now : null),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return affectedRows == 1;
        });
    }

    public async Task<EmailNotificationStatus?> TryTransitionAfterSendFailureAsync(
        Guid id,
        Guid recipientAccountId,
        Guid chatRoomId,
        DateTime createdAtUtc,
        int attemptCount,
        int maxAttempts,
        DateTime retryAtUtc,
        DateTime processedAtUtc,
        string lastError,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.CreateExecutionStrategy()
            .ExecuteAsync<EmailNotificationStatus?>(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);
            await AcquireQueueLockAsync(recipientAccountId, chatRoomId, cancellationToken);

            var hasSupersedingSuccessor = await HasSupersedingSuccessorAsync(
                id,
                recipientAccountId,
                chatRoomId,
                createdAtUtc,
                cancellationToken);
            var nextStatus = hasSupersedingSuccessor || attemptCount >= maxAttempts
                ? EmailNotificationStatus.Failed
                : EmailNotificationStatus.Pending;

            var affectedRows = await _dbContext.EmailNotificationOutboxes
                .Where(outbox =>
                    outbox.Id == id &&
                    outbox.Status == EmailNotificationStatus.Processing &&
                    outbox.AttemptCount == attemptCount)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(outbox => outbox.Status, nextStatus)
                        .SetProperty(outbox => outbox.NotBeforeUtc, retryAtUtc)
                        .SetProperty(
                            outbox => outbox.ProcessedAt,
                            nextStatus == EmailNotificationStatus.Failed ? processedAtUtc : null)
                        .SetProperty(outbox => outbox.LastError, lastError),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return affectedRows == 1 ? nextStatus : null;
        });
    }

    private Task<bool> HasSupersedingSuccessorAsync(
        Guid id,
        Guid recipientAccountId,
        Guid chatRoomId,
        DateTime originalCreatedAtUtc,
        CancellationToken cancellationToken)
    {
        return _dbContext.EmailNotificationOutboxes
            .AsNoTracking()
            .AnyAsync(
                outbox =>
                    outbox.Id != id &&
                    outbox.RecipientAccountId == recipientAccountId &&
                    outbox.ChatRoomId == chatRoomId &&
                    outbox.CreatedAt > originalCreatedAtUtc &&
                    (outbox.Status == EmailNotificationStatus.Pending ||
                     outbox.Status == EmailNotificationStatus.Processing ||
                     outbox.Status == EmailNotificationStatus.Sent),
                cancellationToken);
    }
}
