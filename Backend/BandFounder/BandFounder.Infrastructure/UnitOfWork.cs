using BandFounder.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

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
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
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
        });
    }
}
