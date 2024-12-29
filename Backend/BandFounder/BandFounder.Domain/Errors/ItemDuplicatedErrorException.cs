namespace BandFounder.Domain.Errors;

public class ItemDuplicatedErrorException : DomainErrorException
{
    public ItemDuplicatedErrorException(string? message) : base(message)
    {
    }

    public ItemDuplicatedErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemDuplicatedErrorException()
    {
    }
}