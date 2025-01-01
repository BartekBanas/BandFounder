using BandFounder.Infrastructure.Errors;

namespace BandFounder.Infrastructure.Spotify.Errors;

public class FailedToFetchSpotifyTokenException : InfrastructureErrorException
{
    public FailedToFetchSpotifyTokenException(string? message= "") : base(message)
    {
    }

    public FailedToFetchSpotifyTokenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}