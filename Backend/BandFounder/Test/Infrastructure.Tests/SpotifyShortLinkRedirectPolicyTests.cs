using BandFounder.Infrastructure.Spotify;

namespace Infrastructure.Tests;

public class SpotifyShortLinkRedirectPolicyTests
{
    [Test]
    public void CanFollow_AllowsSpotifyLinkToOpenSpotify()
    {
        var from = new Uri("https://spotify.link/abc123");
        var to = new Uri("https://open.spotify.com/playlist/37i9dQZF1DXcBWIGoYBM5M?si=1");

        Assert.That(SpotifyShortLinkRedirectPolicy.CanFollow(from, to), Is.True);
    }

    [Test]
    public void CanFollow_RejectsASecondSpotifyLinkHop()
    {
        var from = new Uri("https://spotify.link/abc123");
        var to = new Uri("https://spotify.link/other");

        Assert.That(SpotifyShortLinkRedirectPolicy.CanFollow(from, to), Is.False);
    }

    [Test]
    public void CanFollow_RejectsOpenSpotifyToAnArbitraryHost()
    {
        var from = new Uri("https://open.spotify.com/playlist/37i9dQZF1DXcBWIGoYBM5M");
        var to = new Uri("https://evil.example/steal");

        Assert.That(SpotifyShortLinkRedirectPolicy.CanFollow(from, to), Is.False);
    }

    [Test]
    public void CanFollow_RejectsSpotifyLinkToAnArbitraryHost()
    {
        var from = new Uri("https://spotify.link/abc123");
        var to = new Uri("https://evil.example/steal");

        Assert.That(SpotifyShortLinkRedirectPolicy.CanFollow(from, to), Is.False);
    }
}
