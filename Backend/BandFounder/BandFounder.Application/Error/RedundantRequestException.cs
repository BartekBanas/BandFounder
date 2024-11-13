namespace BandFounder.Application.Error;

public class RedundantRequestException : Exception
{
    public RedundantRequestException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    
    public RedundantRequestException(string? message) : base(message)
    {
    }
}