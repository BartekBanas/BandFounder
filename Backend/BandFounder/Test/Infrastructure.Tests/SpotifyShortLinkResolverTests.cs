using System.Net;
using BandFounder.Infrastructure.Spotify.Services;

namespace Infrastructure.Tests;

public class SpotifyShortLinkResolverTests
{
    private const string PlaylistId = "37i9dQZF1DXcBWIGoYBM5M";

    [Test]
    public async Task ResolveAsync_FollowsOneSpotifyLinkRedirectToOpenSpotify()
    {
        var handler = new CallbackHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri($"https://open.spotify.com/playlist/{PlaylistId}?si=abc");
            return response;
        });
        var resolver = new SpotifyShortLinkResolver(new HttpClient(handler));

        var resolved = await resolver.ResolveAsync(new Uri("https://spotify.link/abc123"));

        Assert.That(resolved?.Value, Is.EqualTo(PlaylistId));
        Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        Assert.That(handler.RequestUris[0], Is.EqualTo("https://spotify.link/abc123"));
    }

    [Test]
    public async Task ResolveAsync_DoesNotFollowARedirectToAnotherHost()
    {
        var handler = new CallbackHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("https://evil.example/steal");
            return response;
        });
        var resolver = new SpotifyShortLinkResolver(new HttpClient(handler));

        var resolved = await resolver.ResolveAsync(new Uri("https://spotify.link/abc123"));

        Assert.That(resolved, Is.Null);
        Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ResolveAsync_DoesNotFollowASecondSpotifyLinkHop()
    {
        var handler = new CallbackHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("https://spotify.link/other");
            return response;
        });
        var resolver = new SpotifyShortLinkResolver(new HttpClient(handler));

        var resolved = await resolver.ResolveAsync(new Uri("https://spotify.link/abc123"));

        Assert.That(resolved, Is.Null);
        Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ResolveAsync_DoesNotRequestNonSpotifyLinkUrls()
    {
        var handler = new CallbackHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP should not be called"));
        var resolver = new SpotifyShortLinkResolver(new HttpClient(handler));

        var resolved = await resolver.ResolveAsync(new Uri("https://open.spotify.com/playlist/" + PlaylistId));

        Assert.That(resolved, Is.Null);
        Assert.That(handler.RequestUris, Is.Empty);
    }
}
