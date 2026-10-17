using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class SpotifyPlaylistReadTokenProviderTests
{
    [Test]
    public async Task GetAccessTokenAsync_UsesClientCredentialsByDefault()
    {
        var clientCredentials = Substitute.For<ISpotifyClientCredentialsAccessTokenProvider>();
        var connectionService = Substitute.For<ISpotifyConnectionService>();
        clientCredentials.GetAccessTokenAsync().Returns("app-only-token");
        var provider = new SpotifyPlaylistReadTokenProvider(
            Options.Create(new SpotifyPlaylistReadOptions
            {
                TokenSource = SpotifyPlaylistReadTokenSource.ClientCredentials
            }),
            clientCredentials,
            connectionService);

        var token = await provider.GetAccessTokenAsync();

        Assert.That(token, Is.EqualTo("app-only-token"));
        await connectionService.DidNotReceive().GetAccessTokenAsync(Arg.Any<Guid>());
    }

    [Test]
    public async Task GetAccessTokenAsync_UsesAppOwnerStoredSignInTokenWhenConfigured()
    {
        var ownerId = Guid.NewGuid();
        var clientCredentials = Substitute.For<ISpotifyClientCredentialsAccessTokenProvider>();
        var connectionService = Substitute.For<ISpotifyConnectionService>();
        connectionService.GetAccessTokenAsync(ownerId).Returns("owner-token");
        var provider = new SpotifyPlaylistReadTokenProvider(
            Options.Create(new SpotifyPlaylistReadOptions
            {
                TokenSource = SpotifyPlaylistReadTokenSource.AppOwnerSignIn,
                AppOwnerAccountId = ownerId
            }),
            clientCredentials,
            connectionService);

        var token = await provider.GetAccessTokenAsync();

        Assert.That(token, Is.EqualTo("owner-token"));
        await clientCredentials.DidNotReceive().GetAccessTokenAsync();
    }

    [Test]
    public async Task GetAccessTokenAsync_DoesNotFailOverToAppOwnerWhenClientCredentialsFail()
    {
        var clientCredentials = Substitute.For<ISpotifyClientCredentialsAccessTokenProvider>();
        var connectionService = Substitute.For<ISpotifyConnectionService>();
        clientCredentials.GetAccessTokenAsync().Returns<string>(_ => throw new InvalidOperationException("app-only died"));
        var provider = new SpotifyPlaylistReadTokenProvider(
            Options.Create(new SpotifyPlaylistReadOptions
            {
                TokenSource = SpotifyPlaylistReadTokenSource.ClientCredentials,
                AppOwnerAccountId = Guid.NewGuid()
            }),
            clientCredentials,
            connectionService);

        Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetAccessTokenAsync());
        await connectionService.DidNotReceive().GetAccessTokenAsync(Arg.Any<Guid>());
    }
}
