using BandFounder.Infrastructure.Errors;

namespace BandFounder.Infrastructure.Spotify.Errors;

public class SpotifyAccountNotLinkedError : InfrastructureErrorException
{
    private const string DefaultMessage =
        "Spotify account not linked: Please connect Bandfounder with your Spotify account first.";

    public SpotifyAccountNotLinkedError() : base(DefaultMessage)
    {
    }

    public SpotifyAccountNotLinkedError(string? message) : base(message ?? DefaultMessage)
    {
    }

    public SpotifyAccountNotLinkedError(string? message, Exception? innerException) : base(message ?? DefaultMessage, innerException)
    {
    }
}