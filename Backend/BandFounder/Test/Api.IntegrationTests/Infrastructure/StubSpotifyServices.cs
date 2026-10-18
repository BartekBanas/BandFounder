using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;

namespace Api.IntegrationTests.Infrastructure;

public static class StubSpotifyPlaylists
{
    public static string Id(int n) => n.ToString("D22");

    public static string Url(int n) => $"https://open.spotify.com/playlist/{Id(n)}";

    public static string ArtistId(string playlistId) => $"artist{playlistId}";

    public static string GenreName(string artistId) => $"zzz-txn-{artistId}";
}

public sealed class StubSpotifyPlaylistReadService : ISpotifyPlaylistReadService
{
    public Task<SpotifyPlaylistReadResult> ReadAsync(string input)
    {
        return SpotifyPlaylistParser.Parse(input) switch
        {
            SpotifyPlaylistParseResult.Invalid =>
                Task.FromResult<SpotifyPlaylistReadResult>(new SpotifyPlaylistReadResult.InvalidInput()),
            SpotifyPlaylistParseResult.ShortLink =>
                Task.FromResult<SpotifyPlaylistReadResult>(new SpotifyPlaylistReadResult.NotReadable()),
            SpotifyPlaylistParseResult.PlaylistId playlistId =>
                Task.FromResult<SpotifyPlaylistReadResult>(new SpotifyPlaylistReadResult.Success(
                    Playlist(playlistId.Id.Value))),
        };
    }

    public static SpotifyPlaylistRead Playlist(string spotifyId) => new()
    {
        SpotifyId = spotifyId,
        Name = $"Playlist {spotifyId}",
        Artists =
        [
            new SpotifyPlaylistArtist
            {
                Id = StubSpotifyPlaylists.ArtistId(spotifyId),
                Name = $"Artist {spotifyId}"
            }
        ],
        TrackCount = 1,
        TruncatedAtTrackCap = false
    };
}

public sealed class StubSpotifyPlaylistReadTokenProvider : ISpotifyPlaylistReadTokenProvider
{
    public Task<string> GetAccessTokenAsync() => Task.FromResult("test-token");
}

public sealed class StubSpotifyClient : ISpotifyClient
{
    public Task<SpotifyTokensResponse> RequestAccessTokenAsync(
        SpotifyConnectionDto dto,
        SpotifyAppCredentials spotifyAppCredentials) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");

    public Task<SpotifyTokensResponse> RefreshTokenAsync(
        string refreshToken,
        SpotifyAppCredentials spotifyAppCredentials) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");

    public Task<SpotifyTokensResponse> RequestClientCredentialsTokenAsync(
        SpotifyAppCredentials spotifyAppCredentials) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");

    public Task<List<SpotifyArtistDto>> GetTopArtistsAsync(
        string accessToken,
        int limit,
        string timeRange = SpotifyTimeRange.Default) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");

    public Task<List<SpotifyTrackDto>> GetTopTracksAsync(
        string accessToken,
        int limit,
        string timeRange = SpotifyTimeRange.Default) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");

    public Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(string accessToken) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");

    public Task<List<SpotifyArtistDto>> GetArtistsByIdsAsync(
        string accessToken,
        IReadOnlyList<string> artistIds)
    {
        return Task.FromResult(artistIds.Select(id => new SpotifyArtistDto
        {
            Id = id,
            Name = $"Artist {id}",
            Genres = [StubSpotifyPlaylists.GenreName(id)],
            Popularity = 1
        }).ToList());
    }

    public Task<SpotifyPlaylistRead?> GetPlaylistAsync(
        string accessToken,
        SpotifyPlaylistId playlistId,
        string market) =>
        throw new NotSupportedException("Spotify is stubbed in integration tests.");
}
