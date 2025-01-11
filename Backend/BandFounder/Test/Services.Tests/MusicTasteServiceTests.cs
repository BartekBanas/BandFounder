using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using NSubstitute;

namespace Services.Tests;

public class MusicTasteServiceTests
{
    [Test]
    public async Task GetCommonGenres_ShouldReturnCommonGenresSortedByMinValueDescending()
    {
        // Arrange
        var accountService = Substitute.For<IAccountService>();
        var musicTasteService = Substitute.ForPartsOf<MusicTasteService>(accountService);

        var requesterId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user1 = new Account
        {
            Id = requesterId,
            Name = null,
            PasswordHash = null,
            Email = null,
            DateCreated = default,
            Artists =
            {
                new Artist
                {
                    Id = "Artist1",
                    Name = "Artist1",
                    Genres =
                    [
                        new Genre() { Name = "Rock" },
                        new Genre() { Name = "Jazz" }
                    ]
                },
                new Artist
                {
                    Id = "Artist2",
                    Name = "Artist2",
                    Genres =
                    [
                        new Genre() { Name = "Jazz" },
                        new Genre() { Name = "Blues" }
                    ]
                }
            }
        };
        var user2 = new Account
        {
            Id = targetUserId,
            Name = null,
            PasswordHash = null,
            Email = null,
            DateCreated = default,
            Artists =
            [
                new Artist()
                {
                    Id = "Artist3",
                    Name = "Artist3",
                    Genres =
                    [
                        new Genre() { Name = "Jazz" },
                        new Genre() { Name = "Rock" }
                    ]
                },

                new Artist()
                {
                    Id = "Artist4",
                    Name = "Artist4",
                    Genres =
                    [
                        new Genre() { Name = "Classical" },
                        new Genre() { Name = "Jazz" }
                    ]
                }
            ]
        };

        accountService.GetDetailedAccount(requesterId).Returns(user1);
        accountService.GetDetailedAccount(targetUserId).Returns(user2);

        // Act
        var result = await musicTasteService.GetCommonGenres(requesterId, targetUserId);

        // Assert
        var expectedGenres = new List<string> { "Jazz", "Rock" };

        Assert.That(result, Is.EqualTo(expectedGenres));
    }
    
     [Test]
    public async Task GetCommonArtists_ShouldReturnCommonArtists()
    {
        // Arrange
        var accountService = Substitute.For<IAccountService>();
        var musicTasteService = Substitute.ForPartsOf<MusicTasteService>(accountService);

        var requesterId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user1 = new Account
        {
            Id = requesterId,
            Artists =
            [
                new Artist()
                {
                    Id = "Artist1",
                    Name = "Artist1"
                },

                new Artist()
                {
                    Id = "Artist2",
                    Name = "Artist2"
                }
            ],
            Name = null,
            PasswordHash = null,
            Email = null
        };

        var user2 = new Account
        {
            Name = null,
            PasswordHash = null,
            Email = null
        };
        user2.Id = targetUserId;
        user2.Artists =
        [
            new Artist()
            {
                Id = "Artist2",
                Name = "Artist2"
            },

            new Artist()
            {
                Id = "Artist3",
                Name = "Artist3"
            }
        ];

        accountService.GetDetailedAccount(requesterId).Returns(user1);
        accountService.GetDetailedAccount(targetUserId).Returns(user2);

        // Act
        var result = await musicTasteService.GetCommonArtists(requesterId, targetUserId);

        // Assert
        var expectedArtists = new List<string> { "Artist2" };
        Assert.That(result, Is.EqualTo(expectedArtists));
    }

    [Test]
    public void GetWagedGenres_ShouldReturnGenresWithCorrectWeights()
    {
        // Arrange
        var musicTasteService = new MusicTasteService(Substitute.For<IAccountService>());

        var user = new Account
        {
            Artists =
            [
                new Artist()
                {
                    Genres =
                    [
                        new Genre()
                        {
                            Name = "Jazz"
                        },

                        new Genre()
                        {
                            Name = "Blues"
                        }
                    ],
                    Id = null,
                    Name = null
                },

                new Artist
                {
                    Genres =
                    [
                        new Genre()
                        {
                            Name = "Rock"
                        },

                        new Genre()
                        {
                            Name = "Jazz"
                        }
                    ],
                    Id = null,
                    Name = null
                }
                // Act

            ],
            Name = null,
            PasswordHash = null,
            Email = null
        };

        // Act
        var result = musicTasteService.GetWagedGenres(user);

        // Assert
        var expectedGenres = new Dictionary<string, int>
        {
            { "Jazz", 2 },
            { "Rock", 1 },
            { "Blues", 1 }
        };
        Assert.That(result, Is.EqualTo(expectedGenres));
    }

    [Test]
    public async Task CompareMusicTasteAsync_ShouldReturnCorrectSimilarityScore()
    {
        // Arrange
        var accountService = Substitute.For<IAccountService>();
        var musicTasteService = Substitute.ForPartsOf<MusicTasteService>(accountService);

        var requesterId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user1 = new Account
        {
            Id = requesterId,
            Artists =
            [
                new Artist()
                {
                    Id = "Artist1",
                    Genres =
                    [
                        new Genre()
                        {
                            Name = "Rock"
                        },

                        new Genre()
                        {
                            Name = "Jazz"
                        }
                    ],
                    Name = null
                }
            ],
            Name = null,
            PasswordHash = null,
            Email = null
        };

        var user2 = new Account
        {
            Id = targetUserId,
            Artists =
            [
                new Artist
                {
                    Id = "Artist1",
                    Genres =
                    [
                        new Genre()
                        {
                            Name = "Rock"
                        },

                        new Genre()
                        {
                            Name = "Classical"
                        }
                    ],
                    Name = null
                }

            ],
            Name = null,
            PasswordHash = null,
            Email = null
        };

        accountService.GetDetailedAccount(requesterId).Returns(user1);
        accountService.GetDetailedAccount(targetUserId).Returns(user2);

        // Act
        var result = await musicTasteService.CompareMusicTasteAsync(requesterId, targetUserId);

        // Assert
        var expectedScore = 4; // 1 common genre ("Rock") + 1 common artist ("Artist1") * 3
        Assert.That(result, Is.EqualTo(expectedScore));
    }
}