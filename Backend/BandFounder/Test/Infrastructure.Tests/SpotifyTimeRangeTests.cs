using BandFounder.Infrastructure.Spotify;

namespace Infrastructure.Tests;

[TestFixture]
public class SpotifyTimeRangeTests
{
    [TestCase(null, SpotifyTimeRange.Default)]
    [TestCase("", SpotifyTimeRange.Default)]
    [TestCase("   ", SpotifyTimeRange.Default)]
    [TestCase("unknown", SpotifyTimeRange.Default)]
    [TestCase("short_term", SpotifyTimeRange.ShortTerm)]
    [TestCase("medium_term", SpotifyTimeRange.MediumTerm)]
    [TestCase("long_term", SpotifyTimeRange.LongTerm)]
    [TestCase("  LONG_TERM  ", SpotifyTimeRange.LongTerm)]
    [TestCase("Short_Term", SpotifyTimeRange.ShortTerm)]
    public void Normalize_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.That(SpotifyTimeRange.Normalize(input), Is.EqualTo(expected));
    }

    [TestCase(null, 50)]
    [TestCase(-10, 1)]
    [TestCase(1, 1)]
    [TestCase(25, 25)]
    [TestCase(50, 50)]
    [TestCase(51, 50)]
    public void ClampLimit_ClampsBetweenOneAndFifty(int? input, int expected)
    {
        Assert.That(SpotifyTimeRange.ClampLimit(input), Is.EqualTo(expected));
    }

    [Test]
    public void ClampLimit_UsesProvidedDefault_WhenLimitOmitted()
    {
        Assert.That(SpotifyTimeRange.ClampLimit(null, 10), Is.EqualTo(10));
    }

    [Test]
    public void ClampLimit_ClampsProvidedDefault()
    {
        Assert.That(SpotifyTimeRange.ClampLimit(null, 500), Is.EqualTo(50));
    }
}
