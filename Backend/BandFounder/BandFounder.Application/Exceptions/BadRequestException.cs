namespace BandFounder.Application.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    
    public BadRequestException(string? message) : base(message)
    {
    }
}