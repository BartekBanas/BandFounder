using System.Text.Json;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;

namespace Infrastructure.Tests;

public class SpotifyPlaylistIdTests
{
    private const string Id = "37i9dQZF1DXcBWIGoYBM5M";

    [TestCase(Id)]
    [TestCase("  " + Id + "  ")]
    [TestCase("spotify:playlist:" + Id)]
    [TestCase("spotify:user:someone:playlist:" + Id)]
    [TestCase("https://open.spotify.com/playlist/" + Id)]
    [TestCase("https://open.spotify.com/playlist/" + Id + "?si=abc123&pt=xyz")]
    [TestCase("https://open.spotify.com/intl-pl/playlist/" + Id + "?si=abc")]
    [TestCase("https://open.spotify.com/embed/playlist/" + Id)]
    [TestCase("open.spotify.com/playlist/" + Id)]
    public void TryParse_AcceptsLinksUrisAndBareIds(string input)
    {
        Assert.That(SpotifyPlaylistId.TryParse(input, out var playlistId), Is.True);
        Assert.That(playlistId, Is.EqualTo(Id));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not a link")]
    [TestCase("https://open.spotify.com/album/" + Id)]
    [TestCase("https://open.spotify.com/playlist/tooShort")]
    [TestCase("https://open.spotify.com/playlist/" + Id + "extra")]
    public void TryParse_RejectsAnythingElse(string? input)
    {
        Assert.That(SpotifyPlaylistId.TryParse(input, out _), Is.False);
    }

    [Test]
    public void PlaylistItem_ReadsNewItemField_AndFallsBackToDeprecatedTrackField()
    {
        const string json = """
            {
              "items": [
                { "added_by": { "id": "alice" }, "is_local": false,
                  "item": { "id": "t1", "name": "New Shape", "type": "track", "artists": [{ "id": "a1", "name": "A" }] } },
                { "added_by": { "id": "bob" }, "is_local": false,
                  "track": { "id": "t2", "name": "Old Shape", "type": "track", "artists": [{ "id": "a2", "name": "B" }] } },
                { "added_by": null, "is_local": false, "item": null }
              ],
              "next": null,
              "total": 3
            }
            """;

        var page = JsonSerializer.Deserialize<SpotifyPagingResponse<SpotifyPlaylistItemDto?>>(json)!;

        Assert.That(page.Items[0]!.Content!.Name, Is.EqualTo("New Shape"));
        Assert.That(page.Items[0]!.AddedBy!.Id, Is.EqualTo("alice"));
        Assert.That(page.Items[1]!.Content!.Name, Is.EqualTo("Old Shape"));
        Assert.That(page.Items[2]!.Content, Is.Null);
        Assert.That(page.Total, Is.EqualTo(3));
    }

    [Test]
    public void Playlist_ReadsItemCountFromEitherField()
    {
        var newShape = JsonSerializer.Deserialize<SpotifyPlaylistDto>("""{ "id": "p1", "name": "N", "items": { "total": 7 } }""")!;
        var oldShape = JsonSerializer.Deserialize<SpotifyPlaylistDto>("""{ "id": "p2", "name": "O", "tracks": { "total": 4 } }""")!;

        Assert.That(newShape.ItemCount, Is.EqualTo(7));
        Assert.That(oldShape.ItemCount, Is.EqualTo(4));
    }
}
