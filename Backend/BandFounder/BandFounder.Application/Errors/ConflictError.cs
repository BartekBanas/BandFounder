namespace BandFounder.Application.Errors;

public class ConflictError : ErrorException
{
    public ConflictError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}