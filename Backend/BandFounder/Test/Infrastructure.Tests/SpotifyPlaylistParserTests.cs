using BandFounder.Infrastructure.Spotify;

namespace Infrastructure.Tests;

public class SpotifyPlaylistParserTests
{
    private const string PlaylistId = "37i9dQZF1DXcBWIGoYBM5M";

    [Test]
    public void Parse_AcceptsBareId()
    {
        var result = SpotifyPlaylistParser.Parse($"  {PlaylistId}  ");

        Assert.That(result, Is.TypeOf<SpotifyPlaylistParseResult.PlaylistId>());
        Assert.That(((SpotifyPlaylistParseResult.PlaylistId)result).Id.Value, Is.EqualTo(PlaylistId));
    }

    [Test]
    public void Parse_AcceptsOpenSpotifyUrlWithShareQuery()
    {
        var result = SpotifyPlaylistParser.Parse($"https://open.spotify.com/playlist/{PlaylistId}?si=abc123");

        Assert.That(result, Is.EqualTo(new SpotifyPlaylistParseResult.PlaylistId(ParseId())));
    }

    [Test]
    public void Parse_AcceptsIntlOpenSpotifyUrl()
    {
        var result = SpotifyPlaylistParser.Parse($"https://open.spotify.com/intl-de/playlist/{PlaylistId}");

        Assert.That(result, Is.EqualTo(new SpotifyPlaylistParseResult.PlaylistId(ParseId())));
    }

    [Test]
    public void Parse_AcceptsPlaylistUri()
    {
        var result = SpotifyPlaylistParser.Parse($"spotify:playlist:{PlaylistId}");

        Assert.That(result, Is.EqualTo(new SpotifyPlaylistParseResult.PlaylistId(ParseId())));
    }

    [Test]
    public void Parse_LeavesSpotifyLinkForBackendResolution()
    {
        var result = SpotifyPlaylistParser.Parse("https://spotify.link/abc123");

        Assert.That(result, Is.TypeOf<SpotifyPlaylistParseResult.ShortLink>());
        Assert.That(((SpotifyPlaylistParseResult.ShortLink)result).Url.Host, Is.EqualTo("spotify.link"));
    }

    [Test]
    public void Parse_NormalizesSchemeLessSpotifyLink()
    {
        var result = SpotifyPlaylistParser.Parse("spotify.link/abc123");

        Assert.That(result, Is.EqualTo(new SpotifyPlaylistParseResult.ShortLink(new Uri("https://spotify.link/abc123"))));
    }

    [TestCase("")]
    [TestCase("not-a-playlist")]
    [TestCase("https://open.spotify.com/track/4cOdK2wGLETKBe3p0IXgQd")]
    [TestCase("https://example.com/playlist/37i9dQZF1DXcBWIGoYBM5M")]
    public void Parse_RejectsInvalidInput(string input)
    {
        Assert.That(SpotifyPlaylistParser.Parse(input), Is.EqualTo(new SpotifyPlaylistParseResult.Invalid()));
    }

    private static SpotifyPlaylistId ParseId()
    {
        Assert.That(SpotifyPlaylistId.TryParse(PlaylistId, out var playlistId), Is.True);
        return playlistId;
    }
}
