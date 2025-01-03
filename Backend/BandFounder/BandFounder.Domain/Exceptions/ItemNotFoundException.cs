namespace BandFounder.Domain.Exceptions;

public class ItemNotFoundException : DomainException
{
    public ItemNotFoundException(string? message= "") : base(message)
    {
    }

    public ItemNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}