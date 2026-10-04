namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyResourceUnavailableException : Exception
{
    public SpotifyResourceUnavailableException(string message) : base(message)
    {
    }
}
