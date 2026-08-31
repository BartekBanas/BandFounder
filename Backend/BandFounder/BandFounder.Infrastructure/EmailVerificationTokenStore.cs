using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public sealed class EmailVerificationTokenStore : IEmailVerificationStore
{
    private readonly BandFounderDbContext _dbContext;

    public EmailVerificationTokenStore(BandFounderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task IssueForAccountAsync(
        EmailVerificationToken token, DateTime utcNow, bool cancelExisting)
    {
        if (cancelExisting)
        {
            await CancelAllForAccountAsync(token.AccountId, utcNow);
        }

        await _dbContext.EmailVerificationTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<EmailVerificationTokenIssuanceResult> IssueResendAsync(
        Guid accountId,
        Guid tokenId,
        string tokenHash,
        DateTime utcNow,
        TimeSpan tokenTtl,
        TimeSpan resendCooldown)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            // The account row is the per-account issuance lock. Concurrent resends, email
            // changes, and verification updates must serialize before token rotation.
            var account = await _dbContext.Accounts
                .FromSqlInterpolated(
                    $"""SELECT * FROM "Accounts" WHERE "Id" = {accountId} FOR UPDATE""")
                .SingleAsync();

            if (account.EmailVerifiedAt is not null)
            {
                await transaction.CommitAsync();
                return new EmailVerificationTokenIssuanceResult(
                    EmailVerificationTokenIssuanceStatus.AlreadyVerified);
            }

            var latestCreatedAt = await _dbContext.EmailVerificationTokens
                .Where(token => token.AccountId == accountId && token.ConsumedAt == null)
                .MaxAsync(token => (DateTime?)token.CreatedAt);
            var resendAvailableAt = latestCreatedAt?.Add(resendCooldown);

            if (resendAvailableAt is not null && utcNow < resendAvailableAt.Value)
            {
                await transaction.CommitAsync();
                return new EmailVerificationTokenIssuanceResult(
                    EmailVerificationTokenIssuanceStatus.CooldownActive,
                    ResendAvailableAt: resendAvailableAt);
            }

            await CancelAllForAccountAsync(accountId, utcNow);

            var token = new EmailVerificationToken
            {
                Id = tokenId,
                AccountId = accountId,
                TokenHash = tokenHash,
                IntendedEmail = account.Email.Trim().ToLowerInvariant(),
                CreatedAt = utcNow,
                ExpiresAt = utcNow.Add(tokenTtl),
                DeliveryNotBeforeUtc = utcNow
            };
            _dbContext.EmailVerificationTokens.Add(token);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new EmailVerificationTokenIssuanceResult(
                EmailVerificationTokenIssuanceStatus.Issued,
                token,
                utcNow.Add(resendCooldown));
        });
    }

    public async Task<EmailVerificationConfirmationStatus> ConfirmAsync(
        string tokenHash, DateTime utcNow)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var token = await _dbContext.EmailVerificationTokens
                .Include(candidate => candidate.Account)
                .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash);

            if (token is null ||
                !string.Equals(token.Account.Email, token.IntendedEmail, StringComparison.Ordinal))
            {
                await transaction.CommitAsync();
                return EmailVerificationConfirmationStatus.Invalid;
            }

            if (token.Account.EmailVerifiedAt is not null)
            {
                await transaction.CommitAsync();
                return EmailVerificationConfirmationStatus.AlreadyVerified;
            }

            if (token.ConsumedAt is not null || token.ExpiresAt <= utcNow)
            {
                await transaction.CommitAsync();
                return EmailVerificationConfirmationStatus.Invalid;
            }

            token.Account.EmailVerifiedAt = utcNow;
            await CancelAllForAccountAsync(token.AccountId, utcNow);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return EmailVerificationConfirmationStatus.Confirmed;
        });
    }

    public async Task CancelAllForAccountAsync(Guid accountId, DateTime utcNow)
    {
        await _dbContext.EmailVerificationTokens
            .Where(token => token.AccountId == accountId && token.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.ConsumedAt, utcNow)
                .SetProperty(
                    token => token.DeliveryStatus,
                    token => token.DeliveryStatus == EmailNotificationStatus.Pending ||
                             token.DeliveryStatus == EmailNotificationStatus.Processing
                        ? EmailNotificationStatus.Cancelled
                        : token.DeliveryStatus)
                .SetProperty(
                    token => token.DeliveryProcessedAt,
                    token => token.DeliveryStatus == EmailNotificationStatus.Pending ||
                             token.DeliveryStatus == EmailNotificationStatus.Processing
                        ? utcNow
                        : token.DeliveryProcessedAt));
    }

    public async Task<DateTime?> GetLatestActiveCreatedAtAsync(Guid accountId)
    {
        return await _dbContext.EmailVerificationTokens
            .AsNoTracking()
            .Where(token => token.AccountId == accountId && token.ConsumedAt == null)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => (DateTime?)token.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<Guid?> GetNextDueDeliveryIdAsync(DateTime utcNow)
    {
        return _dbContext.EmailVerificationTokens
            .AsNoTracking()
            .Where(token =>
                token.ConsumedAt == null &&
                token.ExpiresAt > utcNow &&
                token.DeliveryStatus == EmailNotificationStatus.Pending &&
                token.DeliveryNotBeforeUtc <= utcNow)
            .OrderBy(token => token.DeliveryNotBeforeUtc)
            .Select(token => (Guid?)token.Id)
            .FirstOrDefaultAsync();
    }

    public Task<EmailVerificationToken?> GetDeliveryAsync(Guid tokenId)
    {
        return _dbContext.EmailVerificationTokens
            .AsNoTracking()
            .Include(token => token.Account)
            .SingleOrDefaultAsync(token => token.Id == tokenId);
    }

    public async Task<bool> TryClaimDeliveryAsync(Guid tokenId, DateTime utcNow)
    {
        var rowsAffected = await _dbContext.EmailVerificationTokens
            .Where(token =>
                token.Id == tokenId &&
                token.ConsumedAt == null &&
                token.ExpiresAt > utcNow &&
                token.DeliveryStatus == EmailNotificationStatus.Pending &&
                token.DeliveryNotBeforeUtc <= utcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.DeliveryStatus, EmailNotificationStatus.Processing)
                .SetProperty(token => token.DeliveryAttemptCount, token => token.DeliveryAttemptCount + 1)
                .SetProperty(token => token.DeliveryLastAttemptAt, utcNow));

        return rowsAffected == 1;
    }

    public async Task<bool> TryMarkDeliverySentAsync(
        Guid tokenId, int attemptCount, DateTime utcNow)
    {
        var rowsAffected = await _dbContext.EmailVerificationTokens
            .Where(token =>
                token.Id == tokenId &&
                token.DeliveryStatus == EmailNotificationStatus.Processing &&
                token.DeliveryAttemptCount == attemptCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.DeliveryStatus, EmailNotificationStatus.Sent)
                .SetProperty(token => token.DeliveryProcessedAt, utcNow)
                .SetProperty(token => token.DeliveryLastError, (string?)null));

        return rowsAffected == 1;
    }

    public async Task<bool> TryMarkDeliveryCancelledAsync(
        Guid tokenId, int attemptCount, DateTime utcNow)
    {
        var rowsAffected = await _dbContext.EmailVerificationTokens
            .Where(token =>
                token.Id == tokenId &&
                token.DeliveryStatus == EmailNotificationStatus.Processing &&
                token.DeliveryAttemptCount == attemptCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.DeliveryStatus, EmailNotificationStatus.Cancelled)
                .SetProperty(token => token.DeliveryProcessedAt, utcNow));

        return rowsAffected == 1;
    }

    public async Task<bool> TryTransitionDeliveryAfterFailureAsync(
        Guid tokenId,
        int attemptCount,
        int maxAttempts,
        DateTime retryAtUtc,
        DateTime utcNow,
        string lastError)
    {
        var nextStatus = attemptCount >= maxAttempts
            ? EmailNotificationStatus.Failed
            : EmailNotificationStatus.Pending;
        var rowsAffected = await _dbContext.EmailVerificationTokens
            .Where(token =>
                token.Id == tokenId &&
                token.DeliveryStatus == EmailNotificationStatus.Processing &&
                token.DeliveryAttemptCount == attemptCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.DeliveryStatus, nextStatus)
                .SetProperty(token => token.DeliveryNotBeforeUtc, retryAtUtc)
                .SetProperty(
                    token => token.DeliveryProcessedAt,
                    nextStatus == EmailNotificationStatus.Failed ? utcNow : null)
                .SetProperty(token => token.DeliveryLastError, lastError));

        return rowsAffected == 1;
    }

    public async Task RecoverStaleDeliveriesAsync(
        DateTime staleBeforeUtc, DateTime utcNow, int maxAttempts)
    {
        await _dbContext.EmailVerificationTokens
            .Where(token =>
                token.DeliveryStatus == EmailNotificationStatus.Processing &&
                token.DeliveryLastAttemptAt < staleBeforeUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    token => token.DeliveryStatus,
                    token => token.ConsumedAt != null ||
                             token.ExpiresAt <= utcNow ||
                             token.DeliveryAttemptCount >= maxAttempts
                        ? EmailNotificationStatus.Failed
                        : EmailNotificationStatus.Pending)
                .SetProperty(token => token.DeliveryNotBeforeUtc, utcNow)
                .SetProperty(
                    token => token.DeliveryProcessedAt,
                    token => token.ConsumedAt != null ||
                             token.ExpiresAt <= utcNow ||
                             token.DeliveryAttemptCount >= maxAttempts
                        ? utcNow
                        : null));
    }
}
