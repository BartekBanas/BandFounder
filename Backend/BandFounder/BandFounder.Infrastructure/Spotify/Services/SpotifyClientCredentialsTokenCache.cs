using System.Diagnostics.CodeAnalysis;

namespace BandFounder.Infrastructure.Spotify.Services;

public sealed class SpotifyClientCredentialsTokenCache
{
    private readonly object _gate = new();
    private string? _accessToken;
    private DateTime _expiresAtUtc;

    public bool TryGetValidToken([NotNullWhen(true)] out string? accessToken)
    {
        lock (_gate)
        {
            if (_accessToken is not null && DateTime.UtcNow < _expiresAtUtc)
            {
                accessToken = _accessToken;
                return true;
            }

            accessToken = null;
            return false;
        }
    }

    public void Set(string accessToken, DateTime expiresAtUtc)
    {
        lock (_gate)
        {
            _accessToken = accessToken;
            _expiresAtUtc = expiresAtUtc;
        }
    }
}
