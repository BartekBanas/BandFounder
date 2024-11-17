using BandFounder.Application.Services;
using BandFounder.Application.Services.Spotify;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Services;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class GetWagedGenresTests
{
    private IRepository<Account> _accountRepositoryMock;
    private IRepository<Artist> _artistRepository;
    private IRepository<Genre> _genreRepository;
    private ISpotifyContentRetriever _spotifyContentRetriever;
    private IAuthenticationService _authenticationServiceMock;
    private ISpotifyContentManager _spotifyContentManager;

    [SetUp]
    public void SetUp()
    {
        _accountRepositoryMock = Substitute.For<IRepository<Account>>();
        _artistRepository = Substitute.For<IRepository<Artist>>();
        _genreRepository = Substitute.For<IRepository<Genre>>();
        _authenticationServiceMock = Substitute.For<IAuthenticationService>();
        _spotifyContentRetriever = Substitute.For<ISpotifyContentRetriever>();
        _spotifyContentManager = new SpotifyContentManager(_spotifyContentRetriever, 
            _authenticationServiceMock, _artistRepository, _accountRepositoryMock, _genreRepository);
    }
    
    [Test]
    public async Task GetWagedGenres_ReturnsSortedGenres_WhenUserHasArtists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = userId,
            Artists = new List<Artist>
            {
                new Artist
                {
                    Id = "artist1",
                    Name = "Artist 1",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        },
                        new Genre
                        {
                            Name = "Pop"
                        }
                    },
                },
                new Artist
                {
                    Id = "artist2",
                    Name = "Artist 2",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        },
                        new Genre
                        {
                            Name = "Jazz"
                        }
                    }
                }
            },
            Name = null,
            PasswordHash = null,
            Email = null
        };
        
        _authenticationServiceMock.GetUserId().Returns(userId);
        _accountRepositoryMock.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);

        // Act
        var result = await _spotifyContentManager.GetWagedGenres();

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));  // Expecting 3 genres: Rock, Pop, Jazz
        Assert.That(result["Rock"], Is.EqualTo(2));
        Assert.That(result["Pop"], Is.EqualTo(1));
        Assert.That(result["Jazz"], Is.EqualTo(1));
    }

    [Test]
    public async Task GetWagedGenres_ReturnsEmptyDictionary_WhenUserHasNoArtists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = userId,
            Artists = new List<Artist>(),
            Name = "Test",
            PasswordHash = null,
            Email = null
        };
        
        _authenticationServiceMock.GetUserId().Returns(userId);
        _accountRepositoryMock.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);

        // Act
        var result = await _spotifyContentManager.GetWagedGenres();

        // Assert
        Assert.That(result.Count, Is.EqualTo(0));  // No genres
    }
    
    [Test]
    public async Task GetWagedGenres_CorrectlyCountsDuplicateGenres()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = userId,
            Artists = new List<Artist>
            {
                new Artist
                {
                    Id = "artist1",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        },
                        new Genre
                        {
                            Name = "Pop"
                        }
                    },
                    Name = null
                },
                new Artist
                {
                    Id = "artist2",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        }
                    },
                    Name = null
                }
            },
            Name = null,
            PasswordHash = null,
            Email = null
        };
    
        _authenticationServiceMock.GetUserId().Returns(userId);
        _accountRepositoryMock.GetOneRequiredAsync(
            userId, Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);

        // Act
        var result = await _spotifyContentManager.GetWagedGenres();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));  // Rock and Pop
        Assert.That(result["Rock"], Is.EqualTo(2));
        Assert.That(result["Pop"], Is.EqualTo(1));
    }
    
    [Test]
    public async Task GetWagedGenres_UsesProvidedUserId_WhenGiven()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var account = new Account
        {
            Id = otherUserId,
            Artists = new List<Artist>
            {
                new Artist
                {
                    Id = "artist1",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        }
                    },
                    Name = null
                }
            },
            Name = "",
            PasswordHash = null,
            Email = null
        };

        _accountRepositoryMock.GetOneRequiredAsync(
            userId, Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);
        _authenticationServiceMock.GetUserId().Returns(userId);  // This userId should be ignored in this case

        // Act
        var result = await _spotifyContentManager.GetWagedGenres(userId);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1, result["Rock"]);
    }

    [Test]
    public async Task GetWagedGenres_ReturnsGenresSortedByCountDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = userId,
            Artists = new List<Artist>
            {
                new Artist
                {
                    Id = "artist1",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        },
                        new Genre
                        {
                            Name = "Jazz"
                        }
                    },
                    Name = null
                },
                new Artist
                {
                    Id = "artist2",
                    Genres = new List<Genre>
                    {
                        new Genre
                        {
                            Name = "Rock"
                        },
                        new Genre
                        {
                            Name = "Pop"
                        }
                    },
                    Name = null
                }
            },
            Name = "",
            PasswordHash = null,
            Email = null
        };

        _authenticationServiceMock.GetUserId().Returns(userId);
        _accountRepositoryMock.GetOneRequiredAsync(
            userId, Arg.Any<string>(), Arg.Any<string[]>()).Returns(account);

        // Act
        var result = await _spotifyContentManager.GetWagedGenres();

        // Assert
        var genreNames = result.Keys.ToList();
        Assert.That(genreNames[0], Is.EqualTo("Rock"));
        Assert.That(genreNames[1], Is.EqualTo("Jazz"));
        Assert.That(genreNames[2], Is.EqualTo("Pop"));
    }
}