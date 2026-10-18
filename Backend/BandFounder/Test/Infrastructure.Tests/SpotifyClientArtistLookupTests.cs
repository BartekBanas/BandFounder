using System.Net;
using BandFounder.Infrastructure.Spotify.Services;

namespace Infrastructure.Tests;

public class SpotifyClientArtistLookupTests
{
    private const string AccessToken = "app-token";

    [Test]
    public async Task GetArtistsByIdsAsync_ReturnsEmptyWithoutCallingSpotify()
    {
        var handler = new CallbackHttpMessageHandler(_ => throw new InvalidOperationException("Spotify should not be called"));
        var client = new SpotifyClient(new HttpClient(handler));

        var artists = await client.GetArtistsByIdsAsync(AccessToken, []);

        Assert.That(artists, Is.Empty);
        Assert.That(handler.RequestUris, Is.Empty);
    }

    [Test]
    public async Task GetArtistsByIdsAsync_SkipsNullEntriesAndMissingIds()
    {
        var handler = new CallbackHttpMessageHandler(_ => JsonHttpResponse.From(
            HttpStatusCode.OK,
            """{"artists":[{"id":"a1","name":"One","genres":["rock"],"popularity":12},null,{"id":"","name":"Nope"}]}"""));
        var client = new SpotifyClient(new HttpClient(handler));

        var artists = await client.GetArtistsByIdsAsync(AccessToken, ["a1", "missing"]);

        Assert.That(artists, Has.Count.EqualTo(1));
        Assert.That(artists[0].Id, Is.EqualTo("a1"));
        Assert.That(artists[0].Genres, Is.EquivalentTo(new[] { "rock" }));
    }

    [Test]
    public async Task GetArtistsByIdsAsync_BatchesRequestsAtFiftyIds()
    {
        var ids = Enumerable.Range(0, SpotifyClient.MaxArtistIdsPerRequest + 1)
            .Select(index => $"id{index:D2}")
            .ToList();
        var handler = new CallbackHttpMessageHandler(request =>
        {
            var requestedIds = request.RequestUri!.Query.TrimStart('?').Split('=')[1].Split(',');
            var artistsJson = string.Join(",", requestedIds.Select(id =>
                $"{{\"id\":\"{id}\",\"name\":\"{id}\",\"genres\":[],\"popularity\":0}}"));
            return JsonHttpResponse.From(HttpStatusCode.OK, $"{{\"artists\":[{artistsJson}]}}");
        });
        var client = new SpotifyClient(new HttpClient(handler));

        var artists = await client.GetArtistsByIdsAsync(AccessToken, ids);

        Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
        Assert.That(artists, Has.Count.EqualTo(ids.Count));
        Assert.That(handler.RequestUris[0], Does.Contain("/v1/artists?ids="));
        Assert.That(handler.RequestUris[0].Split(',')[0], Does.Contain("id00"));
    }
}
