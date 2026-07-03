using BandFounder.Infrastructure.Exceptions;

namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyReauthorizationRequiredException : InfrastructureException
{
    private const string DefaultMessage =
        "Your Spotify connection has expired. Please link your account again.";

    public SpotifyReauthorizationRequiredException() : base(DefaultMessage)
    {
    }

    public SpotifyReauthorizationRequiredException(string? message) : base(message ?? DefaultMessage)
    {
    }
}
