using BandFounder.Application.Services;

namespace Services.Tests;

[TestFixture]
public class SpotifyGenreMatcherTests
{
    [Test]
    public void ArtistMatchesGenre_Contains_CaseInsensitive()
    {
        Assert.That(
            SpotifyGenreMatcher.ArtistMatchesGenre(["progressive metal", "djent"], "Metal"),
            Is.True);
    }

    [Test]
    public void ArtistMatchesGenre_NoMatch_ReturnsFalse()
    {
        Assert.That(
            SpotifyGenreMatcher.ArtistMatchesGenre(["dream pop"], "metal"),
            Is.False);
    }

    [Test]
    public void ArtistMatchesGenre_EmptyQuery_ReturnsFalse()
    {
        Assert.That(
            SpotifyGenreMatcher.ArtistMatchesGenre(["rock"], "   "),
            Is.False);
    }

    [Test]
    public void TrackMatchesGenre_MatchesWhenAnyArtistMatches()
    {
        var genresByArtistId = new Dictionary<string, IReadOnlyList<string>>
        {
            ["a1"] = ["indie rock"],
            ["a2"] = ["dream pop", "shoegaze"],
        };

        Assert.That(
            SpotifyGenreMatcher.TrackMatchesGenre(["a1", "a2"], genresByArtistId, "pop"),
            Is.True);
    }

    [Test]
    public void TrackMatchesGenre_IgnoresMissingArtistIds()
    {
        var genresByArtistId = new Dictionary<string, IReadOnlyList<string>>
        {
            ["a1"] = ["metal"],
        };

        Assert.That(
            SpotifyGenreMatcher.TrackMatchesGenre([null, "missing", "a1"], genresByArtistId, "metal"),
            Is.True);
        Assert.That(
            SpotifyGenreMatcher.TrackMatchesGenre([null, "missing"], genresByArtistId, "metal"),
            Is.False);
    }
}
