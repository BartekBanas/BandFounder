namespace BandFounder.Infrastructure.Errors;

public abstract class InfrastructureErrorException : Exception
{
    protected InfrastructureErrorException()
    {
    }

    protected InfrastructureErrorException(string? message) : base(message)
    {
    }

    protected InfrastructureErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}