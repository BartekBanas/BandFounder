namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyShortLinkResolver
{
    Task<SpotifyPlaylistId?> ResolveAsync(Uri shortLink);
}

public class SpotifyShortLinkResolver(HttpClient httpClient) : ISpotifyShortLinkResolver
{
    public async Task<SpotifyPlaylistId?> ResolveAsync(Uri shortLink)
    {
        if (!SpotifyShortLinkRedirectPolicy.IsSpotifyLink(shortLink))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, shortLink);
        using var response = await httpClient.SendAsync(request);

        var status = (int)response.StatusCode;
        if (status is < 300 or >= 400 || response.Headers.Location is null)
        {
            return null;
        }

        var destination = response.Headers.Location.IsAbsoluteUri
            ? response.Headers.Location
            : new Uri(shortLink, response.Headers.Location);

        if (!SpotifyShortLinkRedirectPolicy.CanFollow(shortLink, destination))
        {
            return null;
        }

        return SpotifyPlaylistId.TryParse(destination.ToString(), out var playlistId)
            ? playlistId
            : null;
    }
}
