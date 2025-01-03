namespace BandFounder.Infrastructure.Errors;

public abstract class InfrastructureException : Exception
{
    protected InfrastructureException()
    {
    }

    protected InfrastructureException(string? message) : base(message)
    {
    }

    protected InfrastructureException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}