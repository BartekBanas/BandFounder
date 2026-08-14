namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyRateLimitExceededException : Exception
{
    public SpotifyRateLimitExceededException()
        : base("Spotify API rate limit exceeded. Please try again shortly.")
    {
    }

    public SpotifyRateLimitExceededException(string message) : base(message)
    {
    }
}
