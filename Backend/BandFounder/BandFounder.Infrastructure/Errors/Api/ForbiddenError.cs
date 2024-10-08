namespace BandFounder.Infrastructure.Errors.Api;

public class ForbiddenError : Exception
{
    public ForbiddenError()
    {
    }

    public ForbiddenError(string? message) : base(message)
    {
    }
}