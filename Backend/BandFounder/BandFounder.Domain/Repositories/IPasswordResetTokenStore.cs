namespace BandFounder.Domain.Repositories;

/// <summary>
/// Account details a valid reset token points at, used to show who the reset applies to.
/// </summary>
public record PasswordResetTokenOwner(Guid AccountId, DateTime ExpiresAt);

public interface IPasswordResetTokenStore
{
    /// <summary>
    /// Looks up the owner of a valid, unexpired reset token without consuming it.
    /// Returns null when the token is unknown, already used or expired.
    /// </summary>
    Task<PasswordResetTokenOwner?> GetOwnerAsync(string tokenHash, DateTime utcNow);

    /// <summary>
    /// Atomically consumes a valid, unexpired reset token.
    /// Returns the account id when successful; otherwise null.
    /// </summary>
    Task<Guid?> TryConsumeAsync(string tokenHash, DateTime utcNow);

    /// <summary>
    /// Marks all unconsumed reset tokens for the account as consumed.
    /// </summary>
    Task ConsumeAllForAccountAsync(Guid accountId, DateTime utcNow);
}
