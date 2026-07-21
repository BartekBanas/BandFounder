using BandFounder.Domain.Repositories;

namespace BandFounder.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BandFounderDbContext _dbContext;

    public UnitOfWork(BandFounderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
