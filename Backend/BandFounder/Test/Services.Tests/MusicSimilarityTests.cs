using BandFounder.Application.Services;

namespace Services.Tests;

[TestFixture]
public class MusicSimilarityTests
{
    [Test]
    public void Score_ShouldCombineGenreWeightsAndSharedArtists()
    {
        var first = new MusicProfile(
            new HashSet<string> { "artist-1", "artist-2" },
            new Dictionary<string, int> { ["Rock"] = 3, ["Jazz"] = 1 });
        var second = new MusicProfile(
            new HashSet<string> { "artist-1", "artist-3" },
            new Dictionary<string, int> { ["Rock"] = 2, ["Pop"] = 4 });

        var score = MusicSimilarity.Score(first, second);

        Assert.That(score, Is.EqualTo(5));
    }

    [Test]
    public void Score_ShouldReturnZeroForEmptyProfiles()
    {
        var empty = new MusicProfile(
            new HashSet<string>(),
            new Dictionary<string, int>());

        Assert.That(MusicSimilarity.Score(empty, empty), Is.Zero);
    }
}
