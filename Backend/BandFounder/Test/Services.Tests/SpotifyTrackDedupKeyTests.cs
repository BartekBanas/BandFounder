using BandFounder.Application.Services;

namespace Services.Tests;

[TestFixture]
public class SpotifyTrackDedupKeyTests
{
    [Test]
    public void Create_IgnoresCaseOuterAndRepeatedWhitespace()
    {
        var a = SpotifyTrackDedupKey.Create("  Dream   On ", ["Aerosmith"]);
        var b = SpotifyTrackDedupKey.Create("dream on", ["  AEROSMITH  "]);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Create_IgnoresArtistOrder()
    {
        var a = SpotifyTrackDedupKey.Create("Under Pressure", ["Queen", "David Bowie"]);
        var b = SpotifyTrackDedupKey.Create("Under Pressure", ["David Bowie", "Queen"]);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Create_DistinguishesDifferentArtistsAndVersions()
    {
        var original = SpotifyTrackDedupKey.Create("Hurt", ["Nine Inch Nails"]);
        var cover = SpotifyTrackDedupKey.Create("Hurt", ["Johnny Cash"]);
        var remaster = SpotifyTrackDedupKey.Create("Hurt - Remastered", ["Nine Inch Nails"]);

        Assert.That(cover, Is.Not.EqualTo(original));
        Assert.That(remaster, Is.Not.EqualTo(original));
    }

    [Test]
    public void Create_DoesNotMergeNameAndArtistBoundaries()
    {
        var a = SpotifyTrackDedupKey.Create("a b", ["c"]);
        var b = SpotifyTrackDedupKey.Create("a", ["b c"]);

        Assert.That(a, Is.Not.EqualTo(b));
    }
}
