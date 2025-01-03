namespace BandFounder.Domain.Exceptions;

public class ItemDuplicatedException : DomainException
{
    public ItemDuplicatedException(string? message) : base(message)
    {
    }

    public ItemDuplicatedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ItemDuplicatedException()
    {
    }
}