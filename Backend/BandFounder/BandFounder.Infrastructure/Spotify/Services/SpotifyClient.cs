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
    Task<SpotifyUserDto> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<SpotifyUserDto?> TryGetUserProfileAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default);
    Task<SpotifyPlaylistDto> GetPlaylistAsync(
        string accessToken,
        string playlistId,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<PlaylistTracksPage> GetPlaylistTracksPagesAsync(
        string accessToken,
        string playlistId,
        CancellationToken cancellationToken = default);
    Task<List<SpotifyPlaylistDto>> GetCurrentUserPlaylistsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
    Task<SpotifyPlaylistDto> CreatePlaylistAsync(
        string accessToken,
        string name,
        string? description,
        CancellationToken cancellationToken = default);
    Task AddTracksToPlaylistAsync(
        string accessToken,
        string playlistId,
        IReadOnlyCollection<string> trackUris,
        CancellationToken cancellationToken = default);
}

public class SpotifyClient : ISpotifyClient
{
    private const string SpotifyAccessTokenUrl = "https://accounts.spotify.com/api/token";
    private const string SpotifyTopArtistsUrl = "https://api.spotify.com/v1/me/top/artists";
    private const string SpotifyTopTracksUrl = "https://api.spotify.com/v1/me/top/tracks";
    private const string SpotifyFollowedArtistsUrl = "https://api.spotify.com/v1/me/following?type=artist";
    private const string SpotifySavedTracksUrl = "https://api.spotify.com/v1/me/tracks";
    private const string SpotifySeveralArtistsUrl = "https://api.spotify.com/v1/artists";
    private const string SpotifyCurrentUserUrl = "https://api.spotify.com/v1/me";
    private const string SpotifyCurrentUserPlaylistsUrl = "https://api.spotify.com/v1/me/playlists";
    private const string SpotifyUsersUrl = "https://api.spotify.com/v1/users";
    private const string SpotifyPlaylistsUrl = "https://api.spotify.com/v1/playlists";

    private const int MaxRateLimitRetries = 5;
    private const int SavedTracksPageSize = 50;
    private const int ArtistsBatchSize = 50;
    private const int PlaylistItemsPageSize = 50;
    private const int CurrentUserPlaylistsPageSize = 50;
    private const int MaxCurrentUserPlaylists = 1000;
    public const int MaxSavedTracks = 7000;
    public const int DefaultSavedTracksMaxPages = MaxSavedTracks / SavedTracksPageSize;
    public const int MaxPlaylistItems = 10_000;
    public const int PlaylistAddBatchSize = 100;

    private const string PlaylistNotFoundMessage =
        "Spotify couldn't find that playlist. Check the link, or make sure the playlist isn't private to someone else.";
    private const string PlaylistItemsForbiddenMessage =
        "Spotify only shares the songs of playlists you own or collaborate on. Ask the owner to invite you as a collaborator, then try again.";
    private const string PlaylistEditForbiddenMessage =
        "You can't add songs to that playlist. Pick one you own or collaborate on.";
    private const string CreatePlaylistFailedMessage =
        "Spotify wouldn't create the playlist.";

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

    public async Task<SpotifyUserDto> GetCurrentUserAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var responseBody = await SendAuthorizedGetAsync(client, SpotifyCurrentUserUrl, accessToken, cancellationToken);
        return JsonSerializer.Deserialize<SpotifyUserDto>(responseBody) ?? throw new InvalidOperationException();
    }

    public async Task<SpotifyUserDto?> TryGetUserProfileAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Spotify removed GET /users/{id} for development-mode apps in February 2026, so this
        // makes a single attempt with no rate-limit retries and treats any failure as "unknown".
        try
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{SpotifyUsersUrl}/{Uri.EscapeDataString(userId)}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SpotifyUserDto>(responseBody);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    public async Task<SpotifyPlaylistDto> GetPlaylistAsync(
        string accessToken,
        string playlistId,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var responseBody = await SendAuthorizedAsync(
            client,
            HttpMethod.Get,
            $"{SpotifyPlaylistsUrl}/{playlistId}?fields=id,name,collaborative,images,owner(id,display_name),external_urls",
            accessToken,
            unavailableMessage: PlaylistNotFoundMessage,
            cancellationToken: cancellationToken);

        return JsonSerializer.Deserialize<SpotifyPlaylistDto>(responseBody) ?? throw new InvalidOperationException();
    }

    public async IAsyncEnumerable<PlaylistTracksPage> GetPlaylistTracksPagesAsync(
        string accessToken,
        string playlistId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"{SpotifyPlaylistsUrl}/{playlistId}/items?limit={PlaylistItemsPageSize}";
        var scannedItemCount = 0;

        using var client = new HttpClient();
        do
        {
            var responseBody = await SendAuthorizedAsync(
                client,
                HttpMethod.Get,
                url,
                accessToken,
                unavailableMessage: PlaylistItemsForbiddenMessage,
                cancellationToken: cancellationToken);
            var responseDto = JsonSerializer.Deserialize<SpotifyPagingResponse<SpotifyPlaylistItemDto?>>(responseBody)
                              ?? throw new InvalidOperationException();

            scannedItemCount += responseDto.Items.Count;

            var entries = responseDto.Items
                .Where(item =>
                    item?.Content is { } track
                    && !item.IsLocal
                    && !track.IsLocal
                    && (track.Type is null || track.Type == "track")
                    && !string.IsNullOrWhiteSpace(track.Id))
                .Select(item => new PlaylistTrackEntry
                {
                    Track = item!.Content!,
                    AddedById = string.IsNullOrWhiteSpace(item.AddedBy?.Id) ? null : item.AddedBy.Id
                })
                .ToList();

            var hasMore = !string.IsNullOrEmpty(responseDto.Next) && scannedItemCount < MaxPlaylistItems;

            yield return new PlaylistTracksPage
            {
                Entries = entries,
                ScannedItemCount = responseDto.Items.Count,
                TotalAvailable = Math.Min(responseDto.Total, MaxPlaylistItems),
                HasMore = hasMore
            };

            if (!hasMore)
            {
                break;
            }

            url = responseDto.Next!;
        } while (true);
    }

    public async Task<List<SpotifyPlaylistDto>> GetCurrentUserPlaylistsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"{SpotifyCurrentUserPlaylistsUrl}?limit={CurrentUserPlaylistsPageSize}";
        var playlists = new List<SpotifyPlaylistDto>();
        var playlistIds = new HashSet<string>(StringComparer.Ordinal);

        using var client = new HttpClient();
        do
        {
            var responseBody = await SendAuthorizedGetAsync(client, url, accessToken, cancellationToken);
            var responseDto = JsonSerializer.Deserialize<SpotifyPagingResponse<SpotifyPlaylistDto?>>(responseBody)
                              ?? throw new InvalidOperationException();

            playlists.AddRange(responseDto.Items
                .Where(playlist => playlist is not null && playlistIds.Add(playlist.Id))
                .Select(playlist => playlist!));

            url = responseDto.Next;
        } while (!string.IsNullOrEmpty(url) && playlists.Count < MaxCurrentUserPlaylists);

        return playlists;
    }

    public async Task<SpotifyPlaylistDto> CreatePlaylistAsync(
        string accessToken,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var responseBody = await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            SpotifyCurrentUserPlaylistsUrl,
            accessToken,
            new { name, description, @public = false, collaborative = false },
            CreatePlaylistFailedMessage,
            cancellationToken);

        return JsonSerializer.Deserialize<SpotifyPlaylistDto>(responseBody) ?? throw new InvalidOperationException();
    }

    public async Task AddTracksToPlaylistAsync(
        string accessToken,
        string playlistId,
        IReadOnlyCollection<string> trackUris,
        CancellationToken cancellationToken = default)
    {
        if (trackUris.Count is 0 or > PlaylistAddBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(trackUris),
                $"Spotify accepts between 1 and {PlaylistAddBatchSize} tracks per request.");
        }

        using var client = new HttpClient();
        await SendAuthorizedAsync(
            client,
            HttpMethod.Post,
            $"{SpotifyPlaylistsUrl}/{playlistId}/items",
            accessToken,
            new { uris = trackUris },
            PlaylistEditForbiddenMessage,
            cancellationToken);
    }

    private static Task<string> SendAuthorizedGetAsync(
        HttpClient client,
        string url,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendAuthorizedAsync(client, HttpMethod.Get, url, accessToken, cancellationToken: cancellationToken);
    }

    /// <param name="unavailableMessage">
    /// When set, a 404 or a non-scope 403 becomes <see cref="SpotifyResourceUnavailableException"/> with this
    /// message, and other Spotify errors become <see cref="SpotifyRequestFailedException"/> carrying Spotify's reason.
    /// When null, a 403 always means missing scopes and other errors throw <see cref="HttpRequestException"/>.
    /// </param>
    private static async Task<string> SendAuthorizedAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string accessToken,
        object? jsonBody = null,
        string? unavailableMessage = null,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= MaxRateLimitRetries; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            if (jsonBody is not null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    Encoding.UTF8,
                    "application/json");
            }

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

            if (response.IsSuccessStatusCode)
            {
                return responseBody;
            }

            var spotifyMessage = ReadSpotifyErrorMessage(responseBody);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var isScopeProblem = spotifyMessage?.Contains("scope", StringComparison.OrdinalIgnoreCase) ?? false;
                if (unavailableMessage is null || isScopeProblem)
                {
                    throw new SpotifyInsufficientScopeException();
                }

                throw new SpotifyResourceUnavailableException(unavailableMessage);
            }

            if (unavailableMessage is null)
            {
                response.EnsureSuccessStatusCode();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new SpotifyResourceUnavailableException(unavailableMessage!);
            }

            throw new SpotifyRequestFailedException(string.IsNullOrWhiteSpace(spotifyMessage)
                ? $"Spotify returned an error ({(int)response.StatusCode})."
                : $"Spotify said: {spotifyMessage}");
        }

        throw new SpotifyRateLimitExceededException();
    }

    private static string? ReadSpotifyErrorMessage(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("error", out var error)
                   && error.ValueKind == JsonValueKind.Object
                   && error.TryGetProperty("message", out var message)
                   && message.ValueKind == JsonValueKind.String
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
