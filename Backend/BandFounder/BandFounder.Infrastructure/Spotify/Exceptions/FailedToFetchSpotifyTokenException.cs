using BandFounder.Infrastructure.Errors;

namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class FailedToFetchSpotifyTokenException : InfrastructureException
{
    public FailedToFetchSpotifyTokenException(string? message= "") : base(message)
    {
    }

    public FailedToFetchSpotifyTokenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}