namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyRequestFailedException : Exception
{
    public SpotifyRequestFailedException(string message) : base(message)
    {
    }
}
