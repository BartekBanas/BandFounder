namespace BandFounder.Domain.Errors;

public class CustomValidationException : DomainErrorException
{
    public CustomValidationException(string? message, Exception? innerException = null) : base(message, innerException)
    {
    }
}