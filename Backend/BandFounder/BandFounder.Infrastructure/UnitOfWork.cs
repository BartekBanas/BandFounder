using System.Data;
using BandFounder.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BandFounder.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private const int SerializableRetryLimit = 8;

    private readonly BandFounderDbContext _dbContext;

    public UnitOfWork(BandFounderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(() => RunOnceAsync(action, IsolationLevel.Unspecified));
    }

    public async Task ExecuteSerializableTransactionAsync(Func<Task> action)
    {
        for (var attempt = 1; attempt <= SerializableRetryLimit; attempt++)
        {
            try
            {
                await RunOnceAsync(action, IsolationLevel.Serializable);
                return;
            }
            catch (Exception exception) when (
                attempt < SerializableRetryLimit && IsSerializationFailure(exception))
            {
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(20 * attempt * attempt + Random.Shared.Next(0, 40));
            }
        }
    }

    private async Task RunOnceAsync(Func<Task> action, IsolationLevel isolationLevel)
    {
        await using var transaction = isolationLevel == IsolationLevel.Unspecified
            ? await _dbContext.Database.BeginTransactionAsync()
            : await _dbContext.Database.BeginTransactionAsync(isolationLevel);

        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // Postgres may already have aborted the transaction (e.g. SSI).
            }

            throw;
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }

            if (current is DbUpdateConcurrencyException)
            {
                return true;
            }
        }

        return false;
    }
}
