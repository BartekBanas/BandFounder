using BandFounder.Domain.Entities;

namespace BandFounder.Domain.Repositories;

public interface IEmailVerificationStore
{
    Task IssueForAccountAsync(
        EmailVerificationToken token, DateTime utcNow, bool cancelExisting);

    Task<EmailVerificationTokenIssuanceResult> IssueResendAsync(
        Guid accountId,
        Guid tokenId,
        string tokenHash,
        DateTime utcNow,
        TimeSpan tokenTtl,
        TimeSpan resendCooldown);

    Task<EmailVerificationConfirmationStatus> ConfirmAsync(string tokenHash, DateTime utcNow);

    Task CancelAllForAccountAsync(Guid accountId, DateTime utcNow);

    Task<DateTime?> GetLatestActiveCreatedAtAsync(Guid accountId);

    Task<Guid?> GetNextDueDeliveryIdAsync(DateTime utcNow);

    Task<EmailVerificationToken?> GetDeliveryAsync(Guid tokenId);

    Task<bool> TryClaimDeliveryAsync(Guid tokenId, DateTime utcNow);

    Task<bool> TryMarkDeliverySentAsync(
        Guid tokenId, int attemptCount, DateTime utcNow);

    Task<bool> TryMarkDeliveryCancelledAsync(
        Guid tokenId, int attemptCount, DateTime utcNow);

    Task<bool> TryTransitionDeliveryAfterFailureAsync(
        Guid tokenId,
        int attemptCount,
        int maxAttempts,
        DateTime retryAtUtc,
        DateTime utcNow,
        string lastError);

    Task RecoverStaleDeliveriesAsync(
        DateTime staleBeforeUtc, DateTime utcNow, int maxAttempts);
}

public enum EmailVerificationTokenIssuanceStatus
{
    Issued,
    AlreadyVerified,
    CooldownActive
}

public sealed record EmailVerificationTokenIssuanceResult(
    EmailVerificationTokenIssuanceStatus Status,
    EmailVerificationToken? Token = null,
    DateTime? ResendAvailableAt = null);

public enum EmailVerificationConfirmationStatus
{
    Confirmed,
    AlreadyVerified,
    Invalid
}
