using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class SpotifyPlaylistReadServiceTests
{
    private const string PlaylistId = "37i9dQZF1DXcBWIGoYBM5M";
    private const string Market = "US";

    private ISpotifyShortLinkResolver _shortLinkResolver;
    private ISpotifyPlaylistReadTokenProvider _tokenProvider;
    private ISpotifyClient _spotifyClient;
    private SpotifyPlaylistReadService _service;

    [SetUp]
    public void SetUp()
    {
        _shortLinkResolver = Substitute.For<ISpotifyShortLinkResolver>();
        _tokenProvider = Substitute.For<ISpotifyPlaylistReadTokenProvider>();
        _spotifyClient = Substitute.For<ISpotifyClient>();
        _tokenProvider.GetAccessTokenAsync().Returns("token");
        _service = new SpotifyPlaylistReadService(
            _shortLinkResolver,
            _tokenProvider,
            _spotifyClient,
            Options.Create(new SpotifyPlaylistReadOptions { Market = Market }));
    }

    [Test]
    public async Task ReadAsync_ReturnsInvalidInputForGarbage()
    {
        var result = await _service.ReadAsync("not-a-playlist");

        Assert.That(result, Is.TypeOf<SpotifyPlaylistReadResult.InvalidInput>());
        await _spotifyClient.DidNotReceive()
            .GetPlaylistAsync(Arg.Any<string>(), Arg.Any<SpotifyPlaylistId>(), Arg.Any<string>());
    }

    [Test]
    public async Task ReadAsync_ReturnsNotReadableWhenPlaylistIsPrivateOrMissing()
    {
        _spotifyClient.GetPlaylistAsync("token", Arg.Any<SpotifyPlaylistId>(), Market)
            .Returns((SpotifyPlaylistRead?)null);

        var result = await _service.ReadAsync(PlaylistId);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistReadResult.NotReadable>());
    }

    [Test]
    public async Task ReadAsync_ReturnsEmptySuccess()
    {
        _spotifyClient.GetPlaylistAsync("token", Arg.Any<SpotifyPlaylistId>(), Market)
            .Returns(new SpotifyPlaylistRead
            {
                SpotifyId = PlaylistId,
                Name = "Empty",
                Artists = [],
                TrackCount = 0,
                TruncatedAtTrackCap = false
            });

        var result = await _service.ReadAsync(PlaylistId);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistReadResult.Success>());
        var success = (SpotifyPlaylistReadResult.Success)result;
        Assert.That(success.Playlist.TrackCount, Is.EqualTo(0));
        Assert.That(success.Playlist.Artists, Is.Empty);
    }

    [Test]
    public async Task ReadAsync_ResolvesShortLinksOnTheBackend()
    {
        Assert.That(SpotifyPlaylistId.TryParse(PlaylistId, out var playlistId), Is.True);
        _shortLinkResolver.ResolveAsync(Arg.Any<Uri>()).Returns(playlistId);
        _spotifyClient.GetPlaylistAsync("token", playlistId, Market)
            .Returns(new SpotifyPlaylistRead
            {
                SpotifyId = PlaylistId,
                Name = "From short link",
                Artists = [new SpotifyPlaylistArtist { Id = "a1", Name = "Artist" }],
                TrackCount = 1,
                TruncatedAtTrackCap = false
            });

        var result = await _service.ReadAsync("https://spotify.link/abc123");

        Assert.That(result, Is.TypeOf<SpotifyPlaylistReadResult.Success>());
        await _shortLinkResolver.Received(1).ResolveAsync(Arg.Is<Uri>(uri => uri.Host == "spotify.link"));
    }

    [Test]
    public async Task ReadAsync_ReturnsNotReadableWhenShortLinkCannotBeResolved()
    {
        _shortLinkResolver.ResolveAsync(Arg.Any<Uri>()).Returns((SpotifyPlaylistId?)null);

        var result = await _service.ReadAsync("https://spotify.link/abc123");

        Assert.That(result, Is.TypeOf<SpotifyPlaylistReadResult.NotReadable>());
        await _spotifyClient.DidNotReceive()
            .GetPlaylistAsync(Arg.Any<string>(), Arg.Any<SpotifyPlaylistId>(), Arg.Any<string>());
    }

    [Test]
    public async Task ReadAsync_PassesConfiguredMarketToSpotify()
    {
        const string market = "GB";
        _service = new SpotifyPlaylistReadService(
            _shortLinkResolver,
            _tokenProvider,
            _spotifyClient,
            Options.Create(new SpotifyPlaylistReadOptions { Market = market }));
        _spotifyClient.GetPlaylistAsync("token", Arg.Any<SpotifyPlaylistId>(), market)
            .Returns(new SpotifyPlaylistRead
            {
                SpotifyId = PlaylistId,
                Name = "GB market",
                Artists = [],
                TrackCount = 0,
                TruncatedAtTrackCap = false
            });

        await _service.ReadAsync(PlaylistId);

        await _spotifyClient.Received(1).GetPlaylistAsync("token", Arg.Any<SpotifyPlaylistId>(), market);
    }
}
