using System.Text.Json;
using BandFounder.Application.Dtos;

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
        var spotifyResponse = JsonSerializer.Deserialize<SpotifyTopArtistsResponse>(jsonString) ?? throw new InvalidOperationException();

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
}