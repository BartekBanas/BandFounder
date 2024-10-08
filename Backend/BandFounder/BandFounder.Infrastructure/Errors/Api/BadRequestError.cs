namespace BandFounder.Infrastructure.Errors.Api;

public class BadRequestError : Exception
{
    public BadRequestError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    
    public BadRequestError(string? message) : base(message)
    {
    }
}