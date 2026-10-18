namespace BandFounder.Domain.Repositories;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action);

    Task ExecuteSerializableTransactionAsync(Func<Task> action);
}
