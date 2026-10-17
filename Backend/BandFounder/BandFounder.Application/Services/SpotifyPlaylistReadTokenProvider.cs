using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.Extensions.Options;

namespace BandFounder.Application.Services;

public interface ISpotifyPlaylistReadTokenProvider
{
    Task<string> GetAccessTokenAsync();
}

public class SpotifyPlaylistReadTokenProvider(
    IOptions<SpotifyPlaylistReadOptions> options,
    ISpotifyClientCredentialsAccessTokenProvider clientCredentialsAccessTokenProvider,
    ISpotifyConnectionService spotifyConnectionService)
    : ISpotifyPlaylistReadTokenProvider
{
    public Task<string> GetAccessTokenAsync()
    {
        var settings = options.Value;
        return settings.TokenSource switch
        {
            SpotifyPlaylistReadTokenSource.ClientCredentials =>
                clientCredentialsAccessTokenProvider.GetAccessTokenAsync(),
            SpotifyPlaylistReadTokenSource.AppOwnerSignIn =>
                spotifyConnectionService.GetAccessTokenAsync(settings.AppOwnerAccountId),
        };
    }
}
