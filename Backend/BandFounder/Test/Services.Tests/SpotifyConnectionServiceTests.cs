using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;
using BandFounder.Infrastructure.Spotify.Services;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class SpotifyConnectionServiceTests
{
    private SpotifyConnectionService _spotifyConnectionService;
    
    private ISpotifyClient _spotifyClient;
    private ISpotifyAppCredentialsService _spotifyAppCredentialsService;
    private IRepository<SpotifyTokens> _spotifyTokensRepository;
    private IRepository<Artist> _artistRepository;
    private IRepository<Account> _accountRepository;
    private IRepository<Genre> _genreRepository;

    [SetUp]
    public void Setup()
    {
        _spotifyClient = Substitute.For<ISpotifyClient>();
        _spotifyAppCredentialsService = Substitute.For<ISpotifyAppCredentialsService>();
        _spotifyAppCredentialsService.LoadCredentials().Returns(new SpotifyAppCredentials
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        });
        _spotifyTokensRepository = Substitute.For<IRepository<SpotifyTokens>>();
        _artistRepository = Substitute.For<IRepository<Artist>>();
        _accountRepository = Substitute.For<IRepository<Account>>();
        _genreRepository = Substitute.For<IRepository<Genre>>();

        _spotifyConnectionService = new SpotifyConnectionService(
            _spotifyClient,
            _spotifyTokensRepository,
            _artistRepository,
            _accountRepository,
            _genreRepository,
            _spotifyAppCredentialsService);
    }

    [Test]
    public async Task SaveRelevantArtists_Should_Save_Artists_When_Not_Already_Exists()
    {
        // Arrange
        var userId = new Guid();
        const string testToken = "testToken";
        var topArtists = new List<SpotifyArtistDto>
        {
            new() { Id = "artist1", Name = "Artist 1", Popularity = 90, Genres = ["rock"] },
        };
        var followedArtists = new List<SpotifyArtistDto>
        {
            new() { Id = "artist2", Name = "Artist 2", Popularity = 80, Genres = ["pop"] },
        };

        var account = new Account
        {
            Id = userId, Artists = [], Email = "test@mail", Name = "test", DateCreated = DateTime.Now,
            PasswordHash = "pass"
        };

        // Mock dependencies
        _accountRepository.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);
        _artistRepository.GetOneAsync(Arg.Any<object>())!.Returns(Task.FromResult<Artist>(null!)); // Artist doesn't exist
        _genreRepository.GetOneAsync(Arg.Any<object>())!.Returns(Task.FromResult<Genre>(null!)); // Genre doesn't exist
        _spotifyTokensRepository.GetOneAsync(userId)!.Returns(Task.FromResult(new SpotifyTokens
        {
            AccountId = userId,
            AccessToken = testToken,
            RefreshToken = "",
            ExpirationDate = DateTime.Now.AddHours(1),
        }));

        _spotifyClient.GetTopArtistsAsync(testToken, Arg.Any<int>(), Arg.Any<string>()).Returns(Task.FromResult(topArtists));
        _spotifyClient.GetFollowedArtistsAsync(testToken).Returns(Task.FromResult(followedArtists));

        // Act
        var savedArtists = await _spotifyConnectionService.SaveRelevantArtists(userId);

        // Assert
        Assert.That(savedArtists, Has.Count.EqualTo(2));
        await _artistRepository.Received(2).CreateAsync(Arg.Any<Artist>()); // Ensure two artists are created
        await _genreRepository.Received(2).CreateAsync(Arg.Any<Genre>()); // Ensure two genres are created
        await _accountRepository.Received(1).SaveChangesAsync(); // Ensure changes are saved
    }

    [Test]
    public async Task SaveRelevantArtists_Should_Not_Add_Existing_Artists()
    {
        // Arrange
        var userId = new Guid();
        const string testToken = "testToken";
        var existingArtist = new Artist
        {
            Id = "artist1",
            Name = "Artist 1",
            Genres = [new Genre
                {
                    Name = "Pop"
                }
            ]
        };
        var topArtists = new List<SpotifyArtistDto>
        {
            new() { Id = "artist1", Name = "Artist 1", Popularity = 90, Genres = ["rock"] }
        };
        var followedArtists = new List<SpotifyArtistDto>
        {
            new() { Id = "artist2", Name = "Artist 2", Popularity = 80, Genres = ["pop"] }
        };

        var account = new Account
        {
            Id = userId, Artists = [existingArtist], Email = "test@mail", Name = "test", DateCreated = DateTime.Now,
            PasswordHash = "pass"
        };

        // Mock dependencies
        _accountRepository.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);
        _artistRepository.GetOneAsync("artist1")!.Returns(Task.FromResult(existingArtist)); // Artist already exists
        _artistRepository.GetOneAsync("artist2")!.Returns(Task.FromResult<Artist>(null!)); // Second artist does not exist
        _spotifyTokensRepository.GetOneAsync(userId)!.Returns(Task.FromResult(new SpotifyTokens
        {
            AccountId = userId,
            AccessToken = testToken,
            RefreshToken = "",
            ExpirationDate = DateTime.Now.AddHours(1),
        }));

        _spotifyClient.GetTopArtistsAsync(testToken, Arg.Any<int>(), Arg.Any<string>()).Returns(Task.FromResult(topArtists));
        _spotifyClient.GetFollowedArtistsAsync(testToken).Returns(Task.FromResult(followedArtists));

        // Act
        var savedArtists = await _spotifyConnectionService.SaveRelevantArtists(userId);

        // Assert
        Assert.That(savedArtists, Has.Count.EqualTo(1)); // Both artists should be returned
        await _artistRepository.Received(2).CreateAsync(Arg.Any<Artist>()); // Only one new artist should be created
        await _accountRepository.Received(1).SaveChangesAsync(); // Ensure changes are saved
    }

    [Test]
    public async Task GetTopArtistsAsync_Should_DeleteTokensAndThrow_WhenRefreshTokenExpired()
    {
        var userId = Guid.NewGuid();
        var expiredTokens = new SpotifyTokens
        {
            AccountId = userId,
            AccessToken = "old-access",
            RefreshToken = "expired-refresh",
            ExpirationDate = DateTime.UtcNow.AddHours(-1),
        };

        _spotifyTokensRepository.GetOneAsync(userId)!.Returns(Task.FromResult(expiredTokens));
        _spotifyClient.RefreshTokenAsync(Arg.Any<string>(), Arg.Any<SpotifyAppCredentials>())
            .Returns<Task<SpotifyTokensResponse>>(_ => throw new SpotifyRefreshTokenExpiredException());

        var ex = Assert.ThrowsAsync<SpotifyReauthorizationRequiredException>(
            () => _spotifyConnectionService.GetTopArtistsAsync(userId));

        Assert.That(ex!.Message, Does.Contain("expired"));
        await _spotifyTokensRepository.Received(1).DeleteOneAsync(userId);
        await _spotifyTokensRepository.Received(1).SaveChangesAsync();
        await _spotifyClient.DidNotReceive().GetTopArtistsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Test]
    public async Task LinkAccountToSpotify_Should_UpdateExistingTokens_WhenAlreadyConnected()
    {
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = userId,
            Artists = [],
            Email = "test@mail",
            Name = "test",
            DateCreated = DateTime.Now,
            PasswordHash = "pass"
        };
        var existingTokens = new SpotifyTokens
        {
            AccountId = userId,
            AccessToken = "old-access",
            RefreshToken = "old-refresh",
            ExpirationDate = DateTime.UtcNow.AddHours(-1),
        };
        var connectionDto = new SpotifyConnectionDto
        {
            AuthorizationCode = "auth-code",
            BaseFrontendAppUrl = "http://localhost/callback"
        };
        var tokenResponse = new SpotifyTokensResponse
        {
            AccessToken = "new-access",
            RefreshToken = "new-refresh",
            ExpiresIn = 3600
        };

        _accountRepository.GetOneRequiredAsync(userId).Returns(account);
        _spotifyTokensRepository.GetOneAsync(userId)!.Returns(Task.FromResult(existingTokens));
        _spotifyClient.RequestAccessTokenAsync(connectionDto, Arg.Any<SpotifyAppCredentials>())
            .Returns(tokenResponse);
        _spotifyClient.GetTopArtistsAsync("new-access", Arg.Any<int>(), Arg.Any<string>()).Returns([]);
        _spotifyClient.GetFollowedArtistsAsync("new-access").Returns([]);

        await _spotifyConnectionService.LinkAccountToSpotify(connectionDto, userId);

        Assert.That(existingTokens.AccessToken, Is.EqualTo("new-access"));
        Assert.That(existingTokens.RefreshToken, Is.EqualTo("new-refresh"));
        await _spotifyTokensRepository.DidNotReceive().CreateAsync(Arg.Any<SpotifyTokens>());
        await _spotifyTokensRepository.Received().SaveChangesAsync();
    }

    [Test]
    public async Task GetLikedTracksByGenreAsync_FiltersTracksByArtistGenreContains()
    {
        var userId = Guid.NewGuid();
        const string testToken = "testToken";

        _spotifyTokensRepository.GetOneAsync(userId)!.Returns(Task.FromResult(new SpotifyTokens
        {
            AccountId = userId,
            AccessToken = testToken,
            RefreshToken = "",
            ExpirationDate = DateTime.UtcNow.AddHours(1),
        }));

        var tracks = new List<SpotifyTrackDto>
        {
            new()
            {
                Id = "t1",
                Name = "Metal Song",
                Artists = [new SpotifySimplifiedArtistDto { Id = "a1", Name = "Band A" }],
                Album = new SpotifyAlbumDto
                {
                    Images = [new SpotifyImageDto { Url = "https://img/1", Width = 64, Height = 64 }]
                }
            },
            new()
            {
                Id = "t2",
                Name = "Pop Song",
                Artists = [new SpotifySimplifiedArtistDto { Id = "a2", Name = "Band B" }]
            },
        };

        _spotifyClient.GetSavedTracksAsync(testToken, Arg.Any<int>()).Returns(new SavedTracksFetchResult
        {
            Tracks = tracks,
            TotalAvailable = 2,
            Truncated = false
        });

        _spotifyClient.GetArtistsByIdsAsync(testToken, Arg.Any<IEnumerable<string>>()).Returns([
            new SpotifyArtistDto { Id = "a1", Name = "Band A", Genres = ["progressive metal"], Popularity = 50 },
            new SpotifyArtistDto { Id = "a2", Name = "Band B", Genres = ["dance pop"], Popularity = 40 },
        ]);

        var result = await _spotifyConnectionService.GetLikedTracksByGenreAsync(userId, "Metal");

        Assert.That(result.Tracks, Has.Count.EqualTo(1));
        Assert.That(result.Tracks[0].Id, Is.EqualTo("t1"));
        Assert.That(result.Tracks[0].AlbumImageUrl, Is.EqualTo("https://img/1"));
        Assert.That(result.ScannedTrackCount, Is.EqualTo(2));
        Assert.That(result.Truncated, Is.False);
    }

    [Test]
    public async Task ScanLikedTracksByGenreAsync_ReportsProgress_AndCachesArtistGenres()
    {
        var userId = Guid.NewGuid();
        const string testToken = "testToken";

        _spotifyTokensRepository.GetOneAsync(userId)!.Returns(Task.FromResult(new SpotifyTokens
        {
            AccountId = userId,
            AccessToken = testToken,
            RefreshToken = "",
            ExpirationDate = DateTime.UtcNow.AddHours(1),
        }));

        var artist1 = new SpotifyArtistDto
        {
            Id = "a1",
            Name = "Band A",
            Genres = ["progressive metal"],
            Popularity = 50
        };
        var artist2 = new SpotifyArtistDto
        {
            Id = "a2",
            Name = "Band B",
            Genres = ["dance pop"],
            Popularity = 40
        };

        _spotifyClient.GetSavedTracksPagesAsync(
                testToken,
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(AsAsyncEnumerable(
            [
                new SavedTracksPage
                {
                    Tracks =
                    [
                        CreateTrack("t1", "Metal Song 1", "a1", "Band A"),
                        CreateTrack("t2", "Metal Song 2", "a1", "Band A")
                    ],
                    TotalAvailable = 4,
                    HasMore = true,
                    Truncated = false
                },
                new SavedTracksPage
                {
                    Tracks =
                    [
                        CreateTrack("t3", "Metal Song 3", "a1", "Band A"),
                        CreateTrack("t4", "Pop Song", "a2", "Band B")
                    ],
                    TotalAvailable = 4,
                    HasMore = false,
                    Truncated = false
                }
            ]));

        _spotifyClient.GetArtistsByIdsAsync(testToken, Arg.Any<IEnumerable<string>>())
            .Returns(callInfo =>
            {
                var artistIds = callInfo.Arg<IEnumerable<string>>().ToHashSet();
                return Task.FromResult(
                    artistIds.Contains("a2") ? new List<SpotifyArtistDto> { artist2 } : new List<SpotifyArtistDto> { artist1 });
            });

        var updates = new List<LikedTracksByGenreProgressUpdate>();
        await foreach (var update in _spotifyConnectionService.ScanLikedTracksByGenreAsync(userId, "metal"))
        {
            updates.Add(update);
        }

        Assert.That(
            updates.Select(update => update.Type),
            Is.EqualTo(new[] { "progress", "progress", "complete" }));
        Assert.That(updates[0].ScannedTrackCount, Is.EqualTo(2));
        Assert.That(updates[0].RemainingTrackCount, Is.EqualTo(2));
        Assert.That(updates[0].FoundMatchCount, Is.EqualTo(2));
        Assert.That(updates[1].ScannedTrackCount, Is.EqualTo(4));
        Assert.That(updates[1].RemainingTrackCount, Is.Zero);
        Assert.That(updates[1].FoundMatchCount, Is.EqualTo(3));
        Assert.That(updates[^1].Tracks, Has.Count.EqualTo(3));

        await _spotifyClient.Received(1).GetArtistsByIdsAsync(
            testToken,
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "a1" })));
        await _spotifyClient.Received(1).GetArtistsByIdsAsync(
            testToken,
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "a2" })));
    }

    private static SpotifyTrackDto CreateTrack(
        string id,
        string name,
        string artistId,
        string artistName)
    {
        return new SpotifyTrackDto
        {
            Id = id,
            Name = name,
            Artists = [new SpotifySimplifiedArtistDto { Id = artistId, Name = artistName }]
        };
    }

    private static async IAsyncEnumerable<SavedTracksPage> AsAsyncEnumerable(
        IEnumerable<SavedTracksPage> pages)
    {
        foreach (var page in pages)
        {
            await Task.Yield();
            yield return page;
        }
    }
}
