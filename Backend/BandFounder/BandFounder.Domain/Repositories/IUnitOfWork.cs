namespace BandFounder.Domain.Repositories;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action);
}
