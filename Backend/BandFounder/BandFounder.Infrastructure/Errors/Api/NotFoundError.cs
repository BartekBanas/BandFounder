namespace BandFounder.Infrastructure.Errors.Api;

public class NotFoundError : Exception
{
    public NotFoundError(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}