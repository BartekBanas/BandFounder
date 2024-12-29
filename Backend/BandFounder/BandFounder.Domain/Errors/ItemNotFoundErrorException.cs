namespace BandFounder.Domain.Errors;

public class ItemNotFoundErrorException : DomainErrorException
{
    public ItemNotFoundErrorException(string? message= "") : base(message)
    {
    }

    public ItemNotFoundErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}