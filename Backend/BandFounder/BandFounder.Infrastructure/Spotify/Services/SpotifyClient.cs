using System.Net;
using System.Text;
using System.Text.Json;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;

namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyClient
{
    Task<SpotifyTokensResponse> RequestAccessTokenAsync(SpotifyConnectionDto dto, SpotifyAppCredentials spotifyAppCredentials);
    Task<SpotifyTokensResponse> RefreshTokenAsync(string refreshToken, SpotifyAppCredentials spotifyAppCredentials);
    Task<SpotifyTokensResponse> RequestClientCredentialsTokenAsync(SpotifyAppCredentials spotifyAppCredentials);
    Task<List<SpotifyArtistDto>> GetTopArtistsAsync(string accessToken, int limit, string timeRange = SpotifyTimeRange.Default);
    Task<List<SpotifyTrackDto>> GetTopTracksAsync(string accessToken, int limit, string timeRange = SpotifyTimeRange.Default);
    Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(string accessToken);
    Task<List<SpotifyArtistDto>> GetArtistsByIdsAsync(string accessToken, IReadOnlyList<string> artistIds);
    Task<SpotifyPlaylistRead?> GetPlaylistAsync(string accessToken, SpotifyPlaylistId playlistId, string market);
}

public class SpotifyClient : ISpotifyClient
{
    private const string SpotifyAccessTokenUrl = "https://accounts.spotify.com/api/token";
    private const string SpotifyTopArtistsUrl = "https://api.spotify.com/v1/me/top/artists";
    private const string SpotifyTopTracksUrl = "https://api.spotify.com/v1/me/top/tracks";
    private const string SpotifyFollowedArtistsUrl = "https://api.spotify.com/v1/me/following?type=artist";
    private const string SpotifyArtistsUrl = "https://api.spotify.com/v1/artists";
    private const string SpotifyPlaylistsUrl = "https://api.spotify.com/v1/playlists";
    public const int MaxArtistIdsPerRequest = 50;

    private readonly HttpClient _httpClient;

    public SpotifyClient() : this(new HttpClient())
    {
    }

    public SpotifyClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SpotifyTokensResponse> RequestAccessTokenAsync(SpotifyConnectionDto dto, SpotifyAppCredentials spotifyAppCredentials)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccessTokenUrl);

        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));

        request.Headers.Add("Authorization", $"Basic {authHeader}");

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", dto.AuthorizationCode },
            { "redirect_uri", dto.BaseFrontendAppUrl }
        });

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var spotifyTokens = JsonSerializer.Deserialize<SpotifyTokensResponse>(responseContent);

        if (spotifyTokens is null)
        {
            throw new FailedToFetchSpotifyTokenException("Failed to fetch Spotify token");
        }

        return spotifyTokens;
    }

    public async Task<SpotifyTokensResponse> RefreshTokenAsync(string refreshToken, SpotifyAppCredentials spotifyAppCredentials)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccessTokenUrl);

        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));

        request.Headers.Add("Authorization", $"Basic {authHeader}");

        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        ]);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize<SpotifyTokenErrorResponse>(responseContent);
            if (errorResponse?.Error == "invalid_grant")
            {
                throw new SpotifyRefreshTokenExpiredException();
            }

            throw new FailedToFetchSpotifyTokenException("Failed to refresh Spotify token");
        }

        var spotifyTokens = JsonSerializer.Deserialize<SpotifyTokensResponse>(responseContent);

        if (spotifyTokens is null)
        {
            throw new FailedToFetchSpotifyTokenException("Failed to refresh Spotify token");
        }

        return spotifyTokens;
    }

    public async Task<List<SpotifyArtistDto>> GetTopArtistsAsync(
        string accessToken,
        int limit,
        string timeRange = SpotifyTimeRange.Default)
    {
        var normalizedRange = SpotifyTimeRange.Normalize(timeRange);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{SpotifyTopArtistsUrl}?limit={limit}&time_range={normalizedRange}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        var responseDto = JsonSerializer.Deserialize<TopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

        return responseDto.Items;
    }

    public async Task<List<SpotifyTrackDto>> GetTopTracksAsync(
        string accessToken,
        int limit,
        string timeRange = SpotifyTimeRange.Default)
    {
        var normalizedRange = SpotifyTimeRange.Normalize(timeRange);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{SpotifyTopTracksUrl}?limit={limit}&time_range={normalizedRange}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        var responseDto = JsonSerializer.Deserialize<TopTracksResponse>(responseBody) ?? throw new InvalidOperationException();

        return responseDto.Items;
    }

    public async Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(string accessToken)
    {
        var url = SpotifyFollowedArtistsUrl;
        var followedArtists = new List<SpotifyArtistDto>();
        var artistIds = new HashSet<string>();

        const int maxRequests = 10;
        var requestCount = 0;

        do
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseDto = JsonSerializer.Deserialize<FollowedArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

            followedArtists.AddRange(responseDto.Artists.Items.Where(artist => artistIds.Add(artist.Id)));

            url = responseDto.Artists.Next;
            requestCount++;

        } while (!string.IsNullOrEmpty(url) && requestCount < maxRequests);

        return followedArtists;
    }

    public async Task<List<SpotifyArtistDto>> GetArtistsByIdsAsync(
        string accessToken,
        IReadOnlyList<string> artistIds)
    {
        var ids = artistIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var artists = new List<SpotifyArtistDto>();
        foreach (var batch in ids.Chunk(MaxArtistIdsPerRequest))
        {
            var url = $"{SpotifyArtistsUrl}?ids={string.Join(",", batch)}";
            using var response = await SpotifyRateLimitedHttp.SendAsync(_httpClient, () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                return request;
            });

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseDto = JsonSerializer.Deserialize<SeveralArtistsResponse>(responseBody)
                              ?? throw new InvalidOperationException();

            foreach (var artist in responseDto.Artists)
            {
                if (artist is { Id.Length: > 0 })
                {
                    artists.Add(artist);
                }
            }
        }

        return artists;
    }

    public async Task<SpotifyTokensResponse> RequestClientCredentialsTokenAsync(SpotifyAppCredentials spotifyAppCredentials)
    {
        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));

        using var response = await SpotifyRateLimitedHttp.SendAsync(_httpClient, () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccessTokenUrl);
            request.Headers.Add("Authorization", $"Basic {authHeader}");
            request.Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]);
            return request;
        });

        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new FailedToFetchSpotifyTokenException("Failed to fetch Spotify client credentials token");
        }

        var spotifyTokens = JsonSerializer.Deserialize<SpotifyTokensResponse>(responseContent);
        if (spotifyTokens is null || string.IsNullOrWhiteSpace(spotifyTokens.AccessToken))
        {
            throw new FailedToFetchSpotifyTokenException("Failed to fetch Spotify client credentials token");
        }

        return spotifyTokens;
    }

    public async Task<SpotifyPlaylistRead?> GetPlaylistAsync(
        string accessToken,
        SpotifyPlaylistId playlistId,
        string market)
    {
        var playlistMarket = SpotifyPlaylistReadOptions.RequireMarket(market);
        var metadata = await GetPlaylistMetadataAsync(accessToken, playlistId, playlistMarket);
        if (metadata is null)
        {
            return null;
        }

        var artistsById = new Dictionary<string, SpotifyPlaylistArtist>();
        var offset = 0;
        var trackCount = 0;
        var truncatedAtTrackCap = false;
        var hasMore = true;

        while (hasMore && trackCount < SpotifyPlaylistReadLimits.MaxTracks)
        {
            var page = await GetPlaylistItemsPageAsync(
                accessToken,
                playlistId,
                offset,
                SpotifyPlaylistReadLimits.ItemsPageSize,
                playlistMarket);
            if (page is null)
            {
                return null;
            }

            foreach (var row in page.Items)
            {
                if (trackCount == SpotifyPlaylistReadLimits.MaxTracks)
                {
                    truncatedAtTrackCap = true;
                    break;
                }

                if (row.IsLocal || row.Item is not { Type: "track" } track)
                {
                    continue;
                }

                trackCount++;
                foreach (var artist in track.Artists)
                {
                    if (string.IsNullOrWhiteSpace(artist.Id) || string.IsNullOrWhiteSpace(artist.Name))
                    {
                        continue;
                    }

                    artistsById.TryAdd(artist.Id, new SpotifyPlaylistArtist
                    {
                        Id = artist.Id,
                        Name = artist.Name
                    });
                }
            }

            if (truncatedAtTrackCap)
            {
                break;
            }

            offset += page.Items.Count;
            hasMore = !string.IsNullOrEmpty(page.Next) && page.Items.Count > 0;
            if (trackCount == SpotifyPlaylistReadLimits.MaxTracks)
            {
                truncatedAtTrackCap = hasMore;
                break;
            }
        }

        return new SpotifyPlaylistRead
        {
            SpotifyId = metadata.Id,
            Name = metadata.Name,
            Artists = artistsById.Values.ToList(),
            TrackCount = trackCount,
            TruncatedAtTrackCap = truncatedAtTrackCap
        };
    }

    private async Task<SpotifyPlaylistMetadataDto?> GetPlaylistMetadataAsync(
        string accessToken,
        SpotifyPlaylistId playlistId,
        string market)
    {
        using var response = await SendPlaylistRequestAsync(
            accessToken,
            $"{SpotifyPlaylistsUrl}/{playlistId.Value}?fields=id,name&market={market}");

        if (IsUnavailable(response.StatusCode))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyPlaylistMetadataDto>(body) ?? throw new InvalidOperationException();
    }

    private async Task<SpotifyPlaylistItemsPageDto?> GetPlaylistItemsPageAsync(
        string accessToken,
        SpotifyPlaylistId playlistId,
        int offset,
        int limit,
        string market)
    {
        using var response = await SendPlaylistRequestAsync(
            accessToken,
            $"{SpotifyPlaylistsUrl}/{playlistId.Value}/items?limit={limit}&offset={offset}&market={market}&fields=items(item(type,artists(id,name)),is_local),next,total");

        if (IsUnavailable(response.StatusCode))
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyPlaylistItemsPageDto>(body) ?? throw new InvalidOperationException();
    }

    private Task<HttpResponseMessage> SendPlaylistRequestAsync(string accessToken, string url)
    {
        return SpotifyRateLimitedHttp.SendAsync(_httpClient, () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            return request;
        });
    }

    private static bool IsUnavailable(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden;
}