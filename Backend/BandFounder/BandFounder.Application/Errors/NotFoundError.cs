namespace BandFounder.Application.Errors;

public class NotFoundError : ErrorException
{
    public NotFoundError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}