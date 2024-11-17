namespace BandFounder.Application.Error;

public class CustomValidationException : Exception
{
    public CustomValidationException(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}