using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.Extensions.Options;

namespace BandFounder.Application.Services;

public abstract record SpotifyPlaylistReadResult
{
    public sealed record Success(SpotifyPlaylistRead Playlist) : SpotifyPlaylistReadResult;

    public sealed record NotReadable : SpotifyPlaylistReadResult;

    public sealed record InvalidInput : SpotifyPlaylistReadResult;
}

public static class SpotifyPlaylistReadErrors
{
    public const string NotReadable = "Can't read this playlist. Make sure it's public.";
    public const string InvalidInput = "Invalid Spotify playlist link";
}

public interface ISpotifyPlaylistReadService
{
    Task<SpotifyPlaylistReadResult> ReadAsync(string input);
}

public class SpotifyPlaylistReadService(
    ISpotifyShortLinkResolver shortLinkResolver,
    ISpotifyPlaylistReadTokenProvider tokenProvider,
    ISpotifyClient spotifyClient,
    IOptions<SpotifyPlaylistReadOptions> options)
    : ISpotifyPlaylistReadService
{
    public async Task<SpotifyPlaylistReadResult> ReadAsync(string input)
    {
        var parsed = SpotifyPlaylistParser.Parse(input);
        return parsed switch
        {
            SpotifyPlaylistParseResult.Invalid => new SpotifyPlaylistReadResult.InvalidInput(),
            SpotifyPlaylistParseResult.PlaylistId playlistId => await FetchAsync(playlistId.Id),
            SpotifyPlaylistParseResult.ShortLink shortLink => await ResolveAndFetchAsync(shortLink.Url),
        };
    }

    private async Task<SpotifyPlaylistReadResult> ResolveAndFetchAsync(Uri shortLink)
    {
        var resolvedId = await shortLinkResolver.ResolveAsync(shortLink);
        if (resolvedId is not { } playlistId)
        {
            return new SpotifyPlaylistReadResult.NotReadable();
        }

        return await FetchAsync(playlistId);
    }

    private async Task<SpotifyPlaylistReadResult> FetchAsync(SpotifyPlaylistId playlistId)
    {
        var accessToken = await tokenProvider.GetAccessTokenAsync();
        var playlist = await spotifyClient.GetPlaylistAsync(accessToken, playlistId, options.Value.Market);
        if (playlist is null)
        {
            return new SpotifyPlaylistReadResult.NotReadable();
        }

        return new SpotifyPlaylistReadResult.Success(playlist);
    }
}
