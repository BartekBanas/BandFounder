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
}