namespace BandFounder.Domain.Errors;

public abstract class DomainErrorException : Exception
{
    protected DomainErrorException()
    {
    }

    protected DomainErrorException(string? message) : base(message)
    {
    }

    protected DomainErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}