namespace BandFounder.Domain.Repositories;

public interface IPasswordResetTokenStore
{
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
