using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyClientCredentialsAccessTokenProvider
{
    Task<string> GetAccessTokenAsync();
}

public class SpotifyClientCredentialsAccessTokenProvider(
    ISpotifyClient spotifyClient,
    ISpotifyAppCredentialsService spotifyAppCredentialsService,
    SpotifyClientCredentialsTokenCache tokenCache)
    : ISpotifyClientCredentialsAccessTokenProvider
{
    public async Task<string> GetAccessTokenAsync()
    {
        if (tokenCache.TryGetValidToken(out var cachedToken))
        {
            return cachedToken;
        }

        var credentials = await spotifyAppCredentialsService.LoadCredentials();
        var tokens = await spotifyClient.RequestClientCredentialsTokenAsync(credentials);
        var expiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(tokens.ExpiresIn - 60, 0));
        tokenCache.Set(tokens.AccessToken, expiresAtUtc);
        return tokens.AccessToken;
    }
}
