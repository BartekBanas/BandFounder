namespace BandFounder.Application.Error;

public class ConflictError : Exception
{
    public ConflictError(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}