using System.Net;
using System.Net.Http.Headers;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Services;

namespace Infrastructure.Tests;

public class SpotifyClientPlaylistTests
{
    private const string AccessToken = "app-token";
    private const string PlaylistId = "37i9dQZF1DXcBWIGoYBM5M";
    private const string DefaultMarket = "US";
    private const int RequiredTrackCap = 1000;
    private const int RequiredItemPageSize = 50;
    private const int ExpectedPagesAtCapWhenEveryRowIsATrack = RequiredTrackCap / RequiredItemPageSize;

    [Test]
    public async Task GetPlaylistAsync_ReturnsEmptyPlaylist()
    {
        var handler = PlaylistHandler(total: 0, itemsOnPage: 0);
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist!.Name, Is.EqualTo("Road trip"));
        Assert.That(playlist.SpotifyId, Is.EqualTo(PlaylistId));
        Assert.That(playlist.TrackCount, Is.EqualTo(0));
        Assert.That(playlist.Artists, Is.Empty);
        Assert.That(playlist.TruncatedAtTrackCap, Is.False);
        AssertItemsRequestsUseMarket(handler, DefaultMarket);
    }

    [Test]
    public async Task GetPlaylistAsync_ReturnsNullWhenPlaylistIsPrivateOrMissing()
    {
        var handler = new CallbackHttpMessageHandler(_ =>
            JsonHttpResponse.From(HttpStatusCode.NotFound, """{"error":{"status":404}}"""));
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Null);
    }

    [Test]
    public async Task GetPlaylistAsync_ReturnsNullWhenSpotifyForbidsThePlaylist()
    {
        var handler = new CallbackHttpMessageHandler(_ =>
            JsonHttpResponse.From(HttpStatusCode.Forbidden, """{"error":{"status":403}}"""));
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Null);
    }

    [Test]
    public async Task GetPlaylistAsync_PutsConfiguredMarketOnItemsRequest()
    {
        const string market = "GB";
        var handler = PlaylistHandler(total: 1, itemsOnPage: 1);
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), market);

        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist!.TrackCount, Is.EqualTo(1));
        AssertMetadataRequestsUseMarket(handler, market);
        AssertItemsRequestsUseMarket(handler, market);
    }

    [Test]
    public void GetPlaylistAsync_ThrowsWhenMarketIsNotATwoLetterCode()
    {
        var handler = PlaylistHandler(total: 0, itemsOnPage: 0);
        var client = new SpotifyClient(new HttpClient(handler));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetPlaylistAsync(AccessToken, Id(), "USA"));

        Assert.That(exception!.Message, Is.EqualTo(SpotifyPlaylistReadOptions.InvalidMarketMessage));
        Assert.That(handler.RequestUris, Is.Empty);
    }

    [Test]
    public async Task GetPlaylistAsync_CapsAtOneThousandTracksAndNotesTruncation()
    {
        var handler = new CallbackHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!;
            if (!IsPlaylistItemsRequest(uri))
            {
                return MetadataResponse();
            }

            var query = Query(uri);
            var offset = int.Parse(query["offset"]);
            var limit = int.Parse(query["limit"]);
            return ItemsResponse(total: 1500, offset, TrackRows(offset, limit));
        });
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist!.TruncatedAtTrackCap, Is.True);
        Assert.That(playlist.TrackCount, Is.EqualTo(RequiredTrackCap));
        Assert.That(playlist.Artists.Select(artist => artist.Id), Does.Contain("artist-0").And.Contain("artist-999"));
        var itemRequestUris = ItemRequestUris(handler);
        Assert.That(itemRequestUris, Has.Count.EqualTo(ExpectedPagesAtCapWhenEveryRowIsATrack));
        Assert.That(handler.RequestUris.Any(uri => uri.Contains("/tracks", StringComparison.Ordinal)), Is.False);
        Assert.That(
            itemRequestUris.All(uri => uri.Contains($"limit={RequiredItemPageSize}", StringComparison.Ordinal)),
            Is.True);
        AssertItemsRequestsUseMarket(handler, DefaultMarket);
    }

    [Test]
    public async Task GetPlaylistAsync_DoesNotCountLocalOrNonTrackRowsTowardCap()
    {
        const int totalItems = 2500;
        var handler = new CallbackHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!;
            if (!IsPlaylistItemsRequest(uri))
            {
                return MetadataResponse();
            }

            var query = Query(uri);
            var offset = int.Parse(query["offset"]);
            var limit = int.Parse(query["limit"]);
            var rows = new List<string>(limit);
            for (var i = 0; i < limit && offset + i < totalItems; i++)
            {
                rows.Add(MixedCapRow(offset + i));
            }

            return ItemsResponse(totalItems, offset, rows);
        });
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist!.TrackCount, Is.EqualTo(RequiredTrackCap));
        Assert.That(playlist.TruncatedAtTrackCap, Is.True);
        Assert.That(ItemRequestUris(handler), Has.Count.EqualTo(40));
        AssertItemsRequestsUseMarket(handler, DefaultMarket);
    }

    [Test]
    public async Task GetPlaylistAsync_DoesNotTruncateWhenPlayableTracksStayUnderCap()
    {
        const int localItemCount = 1000;
        const int playableTrackCount = 10;
        var totalItems = localItemCount + playableTrackCount;
        var handler = new CallbackHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!;
            if (!IsPlaylistItemsRequest(uri))
            {
                return MetadataResponse();
            }

            var query = Query(uri);
            var offset = int.Parse(query["offset"]);
            var limit = int.Parse(query["limit"]);
            var rows = new List<string>();
            for (var i = 0; i < limit && offset + i < totalItems; i++)
            {
                var index = offset + i;
                rows.Add(index < localItemCount ? LocalRow() : TrackRow(index));
            }

            return ItemsResponse(totalItems, offset, rows);
        });
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist!.TrackCount, Is.EqualTo(playableTrackCount));
        Assert.That(playlist.TruncatedAtTrackCap, Is.False);
        Assert.That(ItemRequestUris(handler), Has.Count.EqualTo(21));
        AssertItemsRequestsUseMarket(handler, DefaultMarket);
    }

    [Test]
    public async Task GetPlaylistAsync_RetriesAfter429RetryAfter()
    {
        var metadataAttempts = 0;
        var handler = new CallbackHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!;
            if (!IsPlaylistItemsRequest(uri))
            {
                metadataAttempts++;
                if (metadataAttempts == 1)
                {
                    var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    limited.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
                    return limited;
                }

                return MetadataResponse();
            }

            return ItemsResponse(total: 1, offset: 0, TrackRows(offset: 0, itemCount: 1));
        });
        var client = new SpotifyClient(new HttpClient(handler));

        var playlist = await client.GetPlaylistAsync(AccessToken, Id(), DefaultMarket);

        Assert.That(playlist, Is.Not.Null);
        Assert.That(playlist!.TrackCount, Is.EqualTo(1));
        Assert.That(playlist.Artists, Has.Count.EqualTo(1));
        Assert.That(metadataAttempts, Is.EqualTo(2));
        AssertItemsRequestsUseMarket(handler, DefaultMarket);
    }

    [Test]
    public async Task RequestClientCredentialsTokenAsync_PostsClientCredentialsGrant()
    {
        string? body = null;
        var handler = new CallbackHttpMessageHandler(request =>
        {
            body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonHttpResponse.From(HttpStatusCode.OK, """{"access_token":"app-only","token_type":"Bearer","expires_in":3600}""");
        });
        var client = new SpotifyClient(new HttpClient(handler));

        var tokens = await client.RequestClientCredentialsTokenAsync(new BandFounder.Infrastructure.Spotify.Dto.SpotifyAppCredentials
        {
            ClientId = "id",
            ClientSecret = "secret"
        });

        Assert.That(tokens.AccessToken, Is.EqualTo("app-only"));
        Assert.That(handler.RequestUris[0], Is.EqualTo("https://accounts.spotify.com/api/token"));
        Assert.That(body, Does.Contain("grant_type=client_credentials"));
    }

    private static CallbackHttpMessageHandler PlaylistHandler(int total, int itemsOnPage)
    {
        return new CallbackHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!;
            if (!IsPlaylistItemsRequest(uri))
            {
                return MetadataResponse();
            }

            return ItemsResponse(total, offset: 0, TrackRows(offset: 0, itemsOnPage));
        });
    }

    private static void AssertMetadataRequestsUseMarket(CallbackHttpMessageHandler handler, string market)
    {
        var metadataRequestUris = MetadataRequestUris(handler);
        Assert.That(metadataRequestUris, Is.Not.Empty);
        Assert.That(
            metadataRequestUris.All(uri => uri.Contains($"market={market}", StringComparison.Ordinal)),
            Is.True);
    }

    private static void AssertItemsRequestsUseMarket(CallbackHttpMessageHandler handler, string market)
    {
        var itemRequestUris = ItemRequestUris(handler);
        Assert.That(itemRequestUris, Is.Not.Empty);
        Assert.That(
            itemRequestUris.All(uri => uri.Contains($"market={market}", StringComparison.Ordinal)),
            Is.True);
    }

    private static List<string> ItemRequestUris(CallbackHttpMessageHandler handler) =>
        handler.RequestUris
            .Where(uri => uri.Contains("/items", StringComparison.Ordinal))
            .ToList();

    private static List<string> MetadataRequestUris(CallbackHttpMessageHandler handler) =>
        handler.RequestUris
            .Where(uri => uri.Contains("/playlists/", StringComparison.Ordinal)
                          && !uri.Contains("/items", StringComparison.Ordinal))
            .ToList();

    private static bool IsPlaylistItemsRequest(Uri uri) =>
        uri.AbsolutePath.Contains("/items", StringComparison.Ordinal);

    private static Dictionary<string, string> Query(Uri uri) =>
        uri.Query.TrimStart('?').Split('&')
            .Select(part => part.Split('=', 2))
            .ToDictionary(part => part[0], part => part.Length > 1 ? part[1] : string.Empty);

    private static HttpResponseMessage MetadataResponse() =>
        JsonHttpResponse.From(HttpStatusCode.OK, $$"""{"id":"{{PlaylistId}}","name":"Road trip"}""");

    private static HttpResponseMessage ItemsResponse(int total, int offset, IReadOnlyList<string> rows)
    {
        var next = offset + rows.Count < total
            ? $"""https://api.spotify.com/v1/playlists/{PlaylistId}/items?offset={offset + rows.Count}"""
            : null;
        var nextJson = next is null ? "null" : $"\"{next}\"";
        return JsonHttpResponse.From(
            HttpStatusCode.OK,
            $$"""{"items":[{{string.Join(",", rows)}}],"next":{{nextJson}},"total":{{total}}}""");
    }

    private static List<string> TrackRows(int offset, int itemCount)
    {
        var rows = new List<string>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            rows.Add(TrackRow(offset + i));
        }

        return rows;
    }

    private static string TrackRow(int artistIndex) =>
        "{\"item\":{\"type\":\"track\",\"artists\":[{\"id\":\"artist-" + artistIndex +
        "\",\"name\":\"Artist " + artistIndex + "\"}]}}";

    private static string MixedCapRow(int index)
    {
        if (index % 2 == 1)
        {
            return TrackRow(index);
        }

        if (index % 6 == 0)
        {
            return LocalRow();
        }

        if (index % 6 == 2)
        {
            return NullRow();
        }

        return EpisodeRow();
    }

    private static string LocalRow() =>
        """{"is_local":true,"item":{"type":"track","artists":[{"id":"local","name":"Local"}]}}""";

    private static string NullRow() => """{"item":null}""";

    private static string EpisodeRow() =>
        """{"item":{"type":"episode","artists":[{"id":"ep","name":"Show"}]}}""";

    private static SpotifyPlaylistId Id()
    {
        Assert.That(SpotifyPlaylistId.TryParse(PlaylistId, out var playlistId), Is.True);
        return playlistId;
    }
}
