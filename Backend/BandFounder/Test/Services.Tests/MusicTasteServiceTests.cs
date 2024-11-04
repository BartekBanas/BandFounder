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

        var user1Genres = new Dictionary<string, int>
        {
            { "Rock", 3 },
            { "Jazz", 5 },
            { "Pop", 2 }
        };

        var user2Genres = new Dictionary<string, int>
        {
            { "Rock", 5 },
            { "Jazz", 4 },
            { "Classical", 3 }
        };

        var user1 = new Account
        {
            Id = requesterId,
            Name = null,
            PasswordHash = null,
            Email = null,
            DateCreated = default
        };
        var user2 = new Account
        {
            Id = targetUserId,
            Name = null,
            PasswordHash = null,
            Email = null,
            DateCreated = default
        };

        accountService.GetDetailedAccount(requesterId).Returns(user1);
        accountService.GetDetailedAccount(targetUserId).Returns(user2);

        musicTasteService.When(x => x.GetWagedGenres(user1))
            .DoNotCallBase();
        musicTasteService.GetWagedGenres(user1).Returns(user1Genres);

        musicTasteService.When(x => x.GetWagedGenres(user2))
            .DoNotCallBase();
        musicTasteService.GetWagedGenres(user2).Returns(user2Genres);

        // Act
        var result = await musicTasteService.GetCommonGenres(requesterId, targetUserId);

        // Assert
        var expectedGenres = new List<string> { "Jazz", "Rock" };

        Assert.That(result, Is.EqualTo(expectedGenres));
    }
}