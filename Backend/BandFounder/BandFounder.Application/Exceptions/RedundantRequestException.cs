namespace BandFounder.Application.Exceptions;

public class RedundantRequestException : Exception
{
    public RedundantRequestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    
    public RedundantRequestException(string? message) : base(message)
    {
    }
}