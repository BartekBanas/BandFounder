using BandFounder.Application.Services;
using BandFounder.Application.Services.Spotify;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class SpotifyContentManagerTests
{
    private SpotifyContentManager _spotifyContentManager;
    
    private ISpotifyContentRetriever _spotifyContentRetriever;
    private IAuthenticationService _authenticationService;
    private IRepository<Artist> _artistRepository;
    private IRepository<Account> _accountRepository;
    private IRepository<Genre> _genreRepository;

    [SetUp]
    public void Setup()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _artistRepository = Substitute.For<IRepository<Artist>>();
        _accountRepository = Substitute.For<IRepository<Account>>();
        _genreRepository = Substitute.For<IRepository<Genre>>();
        _spotifyContentRetriever = Substitute.For<ISpotifyContentRetriever>();

        _spotifyContentManager = new SpotifyContentManager(
            _spotifyContentRetriever,
            _authenticationService,
            _artistRepository,
            _accountRepository,
            _genreRepository);
    }

    [Test]
    public async Task SaveRelevantArtists_Should_Save_Artists_When_Not_Already_Exists()
    {
        // Arrange
        var userId = new Guid();
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
        _authenticationService.GetUserId().Returns(userId);
        _accountRepository.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);
        _artistRepository.GetOneAsync(Arg.Any<object>())!.Returns(Task.FromResult<Artist>(null!)); // Artist doesn't exist
        _genreRepository.GetOneAsync(Arg.Any<object>())!.Returns(Task.FromResult<Genre>(null!)); // Genre doesn't exist

        _spotifyContentRetriever.GetTopArtistsAsync(userId).Returns(Task.FromResult(topArtists));
        _spotifyContentRetriever.GetFollowedArtistsAsync(userId).Returns(Task.FromResult(followedArtists));

        // Act
        var savedArtists = await _spotifyContentManager.SaveRelevantArtists();

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
        _authenticationService.GetUserId().Returns(userId);
        _accountRepository.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);
        _artistRepository.GetOneAsync("artist1")!.Returns(Task.FromResult(existingArtist)); // Artist already exists
        _artistRepository.GetOneAsync("artist2")!.Returns(Task.FromResult<Artist>(null!)); // Second artist does not exist

        _spotifyContentRetriever.GetTopArtistsAsync(userId).Returns(Task.FromResult(topArtists));
        _spotifyContentRetriever.GetFollowedArtistsAsync(userId).Returns(Task.FromResult(followedArtists));

        // Act
        var savedArtists = await _spotifyContentManager.SaveRelevantArtists();

        // Assert
        Assert.That(savedArtists, Has.Count.EqualTo(1)); // Both artists should be returned
        await _artistRepository.Received(2).CreateAsync(Arg.Any<Artist>()); // Only one new artist should be created
        await _accountRepository.Received(1).SaveChangesAsync(); // Ensure changes are saved
    }
}