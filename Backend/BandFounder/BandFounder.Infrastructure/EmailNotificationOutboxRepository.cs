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

    public async Task<bool> TryRecoverStaleAsync(
        Guid id,
        DateTime staleBeforeUtc,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.EmailNotificationOutboxes
            .Where(outbox =>
                outbox.Id == id &&
                outbox.Status == EmailNotificationStatus.Processing &&
                outbox.LastAttemptAt < staleBeforeUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outbox => outbox.Status, EmailNotificationStatus.Pending)
                    .SetProperty(outbox => outbox.NotBeforeUtc, now),
                cancellationToken);

        return affectedRows == 1;
    }
}
