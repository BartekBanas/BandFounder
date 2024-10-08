namespace BandFounder.Infrastructure.Errors;

public class ItemNotFoundErrorException : InfrastructureErrorException
{
    public ItemNotFoundErrorException(string? message) : base(message)
    {
    }

    public ItemNotFoundErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemNotFoundErrorException()
    {
    }
}