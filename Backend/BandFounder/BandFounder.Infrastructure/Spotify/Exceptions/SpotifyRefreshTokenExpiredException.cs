using BandFounder.Infrastructure.Exceptions;

namespace BandFounder.Infrastructure.Spotify.Exceptions;

public class SpotifyRefreshTokenExpiredException : InfrastructureException
{
    private const string DefaultMessage = "Spotify refresh token has expired.";

    public SpotifyRefreshTokenExpiredException() : base(DefaultMessage)
    {
    }

    public SpotifyRefreshTokenExpiredException(string? message) : base(message ?? DefaultMessage)
    {
    }
}
