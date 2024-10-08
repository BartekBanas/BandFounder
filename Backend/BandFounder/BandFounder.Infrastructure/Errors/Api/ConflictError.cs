namespace BandFounder.Infrastructure.Errors.Api;

public class ConflictError : Exception
{
    public ConflictError(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}