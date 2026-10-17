using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using NSubstitute;

namespace Infrastructure.Tests;

public class SpotifyClientCredentialsAccessTokenProviderTests
{
    [Test]
    public async Task GetAccessTokenAsync_ReusesCachedToken()
    {
        var spotifyClient = Substitute.For<ISpotifyClient>();
        var credentialsService = Substitute.For<ISpotifyAppCredentialsService>();
        credentialsService.LoadCredentials().Returns(new SpotifyAppCredentials
        {
            ClientId = "id",
            ClientSecret = "secret"
        });
        spotifyClient.RequestClientCredentialsTokenAsync(Arg.Any<SpotifyAppCredentials>())
            .Returns(new SpotifyTokensResponse
            {
                AccessToken = "cached",
                RefreshToken = string.Empty,
                ExpiresIn = 3600
            });
        var provider = new SpotifyClientCredentialsAccessTokenProvider(
            spotifyClient,
            credentialsService,
            new SpotifyClientCredentialsTokenCache());

        var first = await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();

        Assert.That(first, Is.EqualTo("cached"));
        Assert.That(second, Is.EqualTo("cached"));
        await spotifyClient.Received(1).RequestClientCredentialsTokenAsync(Arg.Any<SpotifyAppCredentials>());
    }
}
