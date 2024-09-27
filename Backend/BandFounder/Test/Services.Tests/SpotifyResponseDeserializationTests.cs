using System.Text.Json;
using BandFounder.Application.Dtos.Spotify;

namespace Services.Tests;

public class SpotifyResponseDeserializationTests
{
    private const string SpotifyTopArtistsResponsePath = "./spotifyTopArtistsResponse.json";
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ShouldDeserializeSpotifyTopArtists()
    {
        var jsonString = File.ReadAllText(SpotifyTopArtistsResponsePath);
        var spotifyResponse = JsonSerializer.Deserialize<TopArtistsResponse>(jsonString) ?? throw new InvalidOperationException();

        Assert.That(spotifyResponse, Is.Not.Null);
        Assert.That(spotifyResponse.Items, Is.Not.Null);
        Assert.That(spotifyResponse.Items, Has.Count.EqualTo(5));

        var firstArtist = spotifyResponse.Items[0];
        Assert.That(firstArtist.Name, Is.EqualTo("Ocean Grove"));
        Assert.That(firstArtist.Id, Is.EqualTo("0AlnGjlLLXglk9hnwErYDU"));
        Assert.That(firstArtist.Popularity, Is.EqualTo(43));
        Assert.That(firstArtist.Genres, Does.Contain("australian metalcore"));

        var secondArtist = spotifyResponse.Items[1];
        Assert.That(secondArtist.Name, Is.EqualTo("Our Last Night"));
        Assert.That(secondArtist.Id, Is.EqualTo("00YTqRClk82aMchQQpYMd5"));
        Assert.That(secondArtist.Popularity, Is.EqualTo(63));
        Assert.That(secondArtist.Genres, Does.Contain("american metalcore"));
    }
    
    [Test]
    public void ShouldDeserializeSpotifyFollowedArtistsResponse()
    {
        // Arrange
        var jsonFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "spotifyFollowedArtistsResponse.json");

        // Act
        var json = File.ReadAllText(jsonFilePath);
        var spotifyResponse = JsonSerializer.Deserialize<FollowedArtistsResponse>(json);

        // Assert
        Assert.NotNull(spotifyResponse);
        Assert.NotNull(spotifyResponse.Artists);
        Assert.IsNotEmpty(spotifyResponse.Artists.Items);

        var firstArtist = spotifyResponse.Artists.Items[0];

        Assert.That(firstArtist.Name, Is.EqualTo("Our Last Night"));
        Assert.That(firstArtist.Popularity, Is.EqualTo(63));
        Assert.That(firstArtist.Id, Is.EqualTo("00YTqRClk82aMchQQpYMd5"));

        var secondArtist = spotifyResponse.Artists.Items[1];
        Assert.That(secondArtist.Name, Is.EqualTo("Slipknot"));
        Assert.That(secondArtist.Popularity, Is.EqualTo(80));
        Assert.That(secondArtist.Id, Is.EqualTo("05fG473iIaoy82BF1aGhL8"));
    }
}