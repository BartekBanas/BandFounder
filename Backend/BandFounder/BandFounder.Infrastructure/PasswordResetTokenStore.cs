using BandFounder.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public sealed class PasswordResetTokenStore : IPasswordResetTokenStore
{
    private readonly BandFounderDbContext _dbContext;

    public PasswordResetTokenStore(BandFounderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> TryConsumeAsync(string tokenHash, DateTime utcNow)
    {
        var rowsAffected = await _dbContext.PasswordResetTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.ConsumedAt == null &&
                token.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(token => token.ConsumedAt, utcNow));

        if (rowsAffected == 0)
        {
            return null;
        }

        var accountId = await _dbContext.PasswordResetTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => token.AccountId)
            .FirstAsync();

        return accountId;
    }

    public async Task ConsumeAllForAccountAsync(Guid accountId, DateTime utcNow)
    {
        await _dbContext.PasswordResetTokens
            .Where(token => token.AccountId == accountId && token.ConsumedAt == null)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(token => token.ConsumedAt, utcNow));
    }
}
