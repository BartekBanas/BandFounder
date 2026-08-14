using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;

namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyClient
{
    Task<SpotifyTokensResponse> RequestAccessTokenAsync(SpotifyConnectionDto dto, SpotifyAppCredentials spotifyAppCredentials);
    Task<SpotifyTokensResponse> RefreshTokenAsync(string refreshToken, SpotifyAppCredentials spotifyAppCredentials);
    Task<List<SpotifyArtistDto>> GetTopArtistsAsync(string accessToken, int limit, string timeRange = SpotifyTimeRange.Default);
    Task<List<SpotifyTrackDto>> GetTopTracksAsync(string accessToken, int limit, string timeRange = SpotifyTimeRange.Default);
    Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(string accessToken);
    Task<SavedTracksFetchResult> GetSavedTracksAsync(
        string accessToken,
        int maxPages = SpotifyClient.DefaultSavedTracksMaxPages);
    IAsyncEnumerable<SavedTracksPage> GetSavedTracksPagesAsync(
        string accessToken,
        int maxTracks = SpotifyClient.MaxSavedTracks,
        CancellationToken cancellationToken = default);
    Task<List<SpotifyArtistDto>> GetArtistsByIdsAsync(string accessToken, IEnumerable<string> artistIds);
}

public class SpotifyClient : ISpotifyClient
{
    private const string SpotifyAccessTokenUrl = "https://accounts.spotify.com/api/token";
    private const string SpotifyTopArtistsUrl = "https://api.spotify.com/v1/me/top/artists";
    private const string SpotifyTopTracksUrl = "https://api.spotify.com/v1/me/top/tracks";
    private const string SpotifyFollowedArtistsUrl = "https://api.spotify.com/v1/me/following?type=artist";
    private const string SpotifySavedTracksUrl = "https://api.spotify.com/v1/me/tracks";
    private const string SpotifySeveralArtistsUrl = "https://api.spotify.com/v1/artists";

    private const int MaxRateLimitRetries = 5;
    private const int SavedTracksPageSize = 50;
    private const int ArtistsBatchSize = 50;
    public const int MaxSavedTracks = 7000;
    public const int DefaultSavedTracksMaxPages = MaxSavedTracks / SavedTracksPageSize;

    public async Task<SpotifyTokensResponse> RequestAccessTokenAsync(SpotifyConnectionDto dto, SpotifyAppCredentials spotifyAppCredentials)
    {
        var client = new HttpClient();
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

        var response = await client.SendAsync(request);

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
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccessTokenUrl);

        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));

        request.Headers.Add("Authorization", $"Basic {authHeader}");

        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        ]);

        var response = await client.SendAsync(request);
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
        using var client = new HttpClient();
        var normalizedRange = SpotifyTimeRange.Normalize(timeRange);
        var responseBody = await SendAuthorizedGetAsync(
            client,
            $"{SpotifyTopArtistsUrl}?limit={limit}&time_range={normalizedRange}",
            accessToken);

        var responseDto = JsonSerializer.Deserialize<TopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

        return responseDto.Items;
    }

    public async Task<List<SpotifyTrackDto>> GetTopTracksAsync(
        string accessToken,
        int limit,
        string timeRange = SpotifyTimeRange.Default)
    {
        using var client = new HttpClient();
        var normalizedRange = SpotifyTimeRange.Normalize(timeRange);
        var responseBody = await SendAuthorizedGetAsync(
            client,
            $"{SpotifyTopTracksUrl}?limit={limit}&time_range={normalizedRange}",
            accessToken);

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

        using var client = new HttpClient();
        do
        {
            var responseBody = await SendAuthorizedGetAsync(client, url, accessToken);
            var responseDto = JsonSerializer.Deserialize<FollowedArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

            followedArtists.AddRange(responseDto.Artists.Items.Where(artist => artistIds.Add(artist.Id)));

            url = responseDto.Artists.Next;
            requestCount++;

        } while (!string.IsNullOrEmpty(url) && requestCount < maxRequests);

        return followedArtists;
    }

    public async Task<SavedTracksFetchResult> GetSavedTracksAsync(
        string accessToken,
        int maxPages = DefaultSavedTracksMaxPages)
    {
        var tracks = new List<SpotifyTrackDto>();
        var totalAvailable = 0;
        var truncated = false;

        var maxTracks = Math.Min(
            MaxSavedTracks,
            Math.Max(1, maxPages) * SavedTracksPageSize);

        await foreach (var page in GetSavedTracksPagesAsync(accessToken, maxTracks))
        {
            totalAvailable = page.TotalAvailable;
            tracks.AddRange(page.Tracks);
            truncated = page.Truncated;
        }

        return new SavedTracksFetchResult
        {
            Tracks = tracks,
            TotalAvailable = totalAvailable,
            Truncated = truncated
        };
    }

    public async IAsyncEnumerable<SavedTracksPage> GetSavedTracksPagesAsync(
        string accessToken,
        int maxTracks = MaxSavedTracks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var normalizedMaxTracks = Math.Clamp(maxTracks, 1, MaxSavedTracks);
        var maxPages = (int)Math.Ceiling((double)normalizedMaxTracks / SavedTracksPageSize);
        var url = $"{SpotifySavedTracksUrl}?limit={SavedTracksPageSize}";
        var requestCount = 0;
        var scannedTrackCount = 0;

        using var client = new HttpClient();
        do
        {
            var responseBody = await SendAuthorizedGetAsync(client, url, accessToken, cancellationToken);
            var responseDto = JsonSerializer.Deserialize<SavedTracksResponse>(responseBody)
                              ?? throw new InvalidOperationException();

            var tracks = responseDto.Items
                .Where(item =>
                    item.Track is not null
                    && !item.Track.IsLocal
                    && !string.IsNullOrWhiteSpace(item.Track.Id))
                .Select(item => item.Track!)
                .Take(normalizedMaxTracks - scannedTrackCount)
                .ToList();

            scannedTrackCount += tracks.Count;
            requestCount++;

            var hasMore = !string.IsNullOrEmpty(responseDto.Next);
            var truncated = hasMore && requestCount >= maxPages;

            yield return new SavedTracksPage
            {
                Tracks = tracks,
                TotalAvailable = responseDto.Total,
                HasMore = hasMore,
                Truncated = truncated
            };

            if (!hasMore || truncated || scannedTrackCount >= normalizedMaxTracks)
            {
                break;
            }

            url = responseDto.Next!;
        } while (true);
    }

    public async Task<List<SpotifyArtistDto>> GetArtistsByIdsAsync(string accessToken, IEnumerable<string> artistIds)
    {
        var uniqueIds = artistIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (uniqueIds.Count == 0)
        {
            return [];
        }

        var artists = new List<SpotifyArtistDto>();
        using var client = new HttpClient();

        for (var i = 0; i < uniqueIds.Count; i += ArtistsBatchSize)
        {
            var batch = uniqueIds.Skip(i).Take(ArtistsBatchSize);
            var idsQuery = string.Join(',', batch);
            var responseBody = await SendAuthorizedGetAsync(
                client,
                $"{SpotifySeveralArtistsUrl}?ids={idsQuery}",
                accessToken);

            var responseDto = JsonSerializer.Deserialize<SeveralArtistsResponse>(responseBody)
                              ?? throw new InvalidOperationException();

            artists.AddRange(responseDto.Artists.Where(artist => artist is not null)!);
        }

        return artists;
    }

    private static async Task<string> SendAuthorizedGetAsync(
        HttpClient client,
        string url,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= MaxRateLimitRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt >= MaxRateLimitRetries)
                {
                    throw new SpotifyRateLimitExceededException();
                }

                var retryAfterSeconds = 1;
                if (response.Headers.RetryAfter?.Delta is { } delta)
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(delta.TotalSeconds));
                }
                else if (response.Headers.TryGetValues("Retry-After", out var values)
                         && int.TryParse(values.FirstOrDefault(), out var parsed))
                {
                    retryAfterSeconds = Math.Max(1, parsed);
                }

                await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds), cancellationToken);
                continue;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new SpotifyInsufficientScopeException();
            }

            if (!response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
            }

            return responseBody;
        }

        throw new SpotifyRateLimitExceededException();
    }
}
