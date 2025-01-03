using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class SpotifyConnectionServiceTests
{
    private SpotifyConnectionService _spotifyConnectionService;
    
    private ISpotifyClient _spotifyClient;
    private IRepository<SpotifyTokens> _spotifyTokensRepository;
    private IRepository<Artist> _artistRepository;
    private IRepository<Account> _accountRepository;
    private IRepository<Genre> _genreRepository;

    [SetUp]
    public void Setup()
    {
        _spotifyClient = Substitute.For<ISpotifyClient>();
        _spotifyTokensRepository = Substitute.For<IRepository<SpotifyTokens>>();
        _artistRepository = Substitute.For<IRepository<Artist>>();
        _accountRepository = Substitute.For<IRepository<Account>>();
        _genreRepository = Substitute.For<IRepository<Genre>>();

        _spotifyConnectionService = new SpotifyConnectionService(
            _spotifyClient,
            _spotifyTokensRepository,
            _artistRepository,
            _accountRepository,
            _genreRepository);
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

        _spotifyClient.GetTopArtistsAsync(testToken, Arg.Any<int>()).Returns(Task.FromResult(topArtists));
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

        _spotifyClient.GetTopArtistsAsync(testToken, Arg.Any<int>()).Returns(Task.FromResult(topArtists));
        _spotifyClient.GetFollowedArtistsAsync(testToken).Returns(Task.FromResult(followedArtists));

        // Act
        var savedArtists = await _spotifyConnectionService.SaveRelevantArtists(userId);

        // Assert
        Assert.That(savedArtists, Has.Count.EqualTo(1)); // Both artists should be returned
        await _artistRepository.Received(2).CreateAsync(Arg.Any<Artist>()); // Only one new artist should be created
        await _accountRepository.Received(1).SaveChangesAsync(); // Ensure changes are saved
    }
}