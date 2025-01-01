namespace BandFounder.Domain.Exceptions;

public class CustomValidationException : DomainException
{
    public CustomValidationException(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}