using BandFounder.Infrastructure.Errors;

namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyAccountNotLinkedException : InfrastructureException
{
    private const string DefaultMessage =
        "Spotify account not linked: Please connect Bandfounder with your Spotify account first.";

    public SpotifyAccountNotLinkedException() : base(DefaultMessage)
    {
    }

    public SpotifyAccountNotLinkedException(string? message) : base(message ?? DefaultMessage)
    {
    }

    public SpotifyAccountNotLinkedException(string? message, Exception? innerException) : base(message ?? DefaultMessage, innerException)
    {
    }
}