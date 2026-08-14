namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyInsufficientScopeException : Exception
{
    public SpotifyInsufficientScopeException()
        : base("Spotify account is missing required permissions. Please reconnect your Spotify account.")
    {
    }

    public SpotifyInsufficientScopeException(string message) : base(message)
    {
    }
}
