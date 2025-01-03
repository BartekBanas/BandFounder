using BandFounder.Infrastructure.Errors;

namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyAccountAlreadyConnectedException : InfrastructureException
{
    private const string DefaultMessage = "This account is already connected to a Spotify account.";

    public SpotifyAccountAlreadyConnectedException() : base(DefaultMessage)
    {
    }

    public SpotifyAccountAlreadyConnectedException(string? message) : base(message ?? DefaultMessage)
    {
    }

    public SpotifyAccountAlreadyConnectedException(string? message, Exception? innerException) : 
        base(message ?? DefaultMessage, innerException)
    {
    }
}