namespace BandFounder.Application.Error;

public class BadRequestError : Exception
{
    public BadRequestError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    
    public BadRequestError(string? message) : base(message)
    {
    }
}