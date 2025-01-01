namespace BandFounder.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}