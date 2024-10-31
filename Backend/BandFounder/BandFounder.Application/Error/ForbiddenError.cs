namespace BandFounder.Application.Error;

public class ForbiddenError : Exception
{
    public ForbiddenError()
    {
    }

    public ForbiddenError(string? message) : base(message)
    {
    }
}