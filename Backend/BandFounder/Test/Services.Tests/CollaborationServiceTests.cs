using System.Linq.Expressions;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Services;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using NSubstitute;

namespace Services.Tests;

public class ListingServiceTests
{
    private readonly IChatroomService _chatroomServiceMock = Substitute.For<IChatroomService>();
    private readonly IRepository<Genre> _genreRepositoryMock = Substitute.For<IRepository<Genre>>();
    private readonly IRepository<MusicianRole> _musicianRoleRepositoryMock = Substitute.For<IRepository<MusicianRole>>();
    private readonly IRepository<MusicianSlot> _musicianSlotRepositoryMock = Substitute.For<IRepository<MusicianSlot>>();

    [Test]
    public async Task GetListingsFeedAsync_WithRoleFilter_FiltersListingsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var accountServiceMock = Substitute.For<IAccountService>();
        var authenticationServiceMock = Substitute.For<IAuthenticationService>();
        var musicTasteServiceMock = Substitute.For<IMusicTasteService>();
        var listingRepositoryMock = Substitute.For<IRepository<Listing>>();

        var filterOptions = new FeedFilterOptions { MatchRole = true };
    
        var userAccount = new Account
        {
            Id = userId,
            MusicianRoles = new List<MusicianRole>
            {
                new()
                {
                    Name = "Guitarist"
                }
            },
            Name = null,
            PasswordHash = null,
            Email = null,
            DateCreated = default
        };

        var listings = new List<Listing>
        {
            new()
            {
                MusicianSlots = new List<MusicianSlot>
                {
                    new()
                    {
                        Role = new MusicianRole
                        {
                            Name = "Guitarist"
                        },
                        Status = SlotStatus.Available
                    }
                },
                OwnerId = Guid.NewGuid(),
                Name = null,
                Type = ListingType.Band
            },
            new()
            {
                MusicianSlots = new List<MusicianSlot>
                {
                    new()
                    {
                        Role = new MusicianRole
                        {
                            Name = "Drummer"
                        },
                        Status = SlotStatus.Filled
                    }
                },
                OwnerId = Guid.NewGuid(),
                Name = null,
                Type = ListingType.Band
            }
        };

        authenticationServiceMock.GetUserId().Returns(userId);
        accountServiceMock.GetDetailedAccount(userId).Returns(userAccount);
        listingRepositoryMock.GetAsync(
            Arg.Any<Expression<Func<Listing, bool>>>(),
            Arg.Any<Func<IQueryable<Listing>, IOrderedQueryable<Listing>>>(),
            Arg.Any<string[]>()
        ).Returns(listings);
        musicTasteServiceMock.CompareMusicTasteAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(1);

        var service = new ListingService(
            accountServiceMock,
            authenticationServiceMock,
            musicTasteServiceMock,
            _chatroomServiceMock,
            _genreRepositoryMock,
            _musicianRoleRepositoryMock,
            _musicianSlotRepositoryMock,
            listingRepositoryMock
        );

        // Act
        var result = await service.GetListingsFeedAsync(filterOptions);

        // Assert
        Assert.AreEqual(1, result.Listings.Count);
        Assert.AreEqual(listings[0].Id, result.Listings.First().Listing.Id);
    }

    [TestCase(SlotStatus.Filled, true)]
    [TestCase(SlotStatus.Filled, false)]
    [TestCase(SlotStatus.Available, true)]
    public async Task GetListingsFeedAsync_WithRoleFilter_FiltersOutIncorrectListings(SlotStatus status, bool differentRole)
    {
        // Arrange
        const string roleName = "Guitarist";
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var accountServiceMock = Substitute.For<IAccountService>();
        var authenticationServiceMock = Substitute.For<IAuthenticationService>();
        var musicTasteServiceMock = Substitute.For<IMusicTasteService>();
        var listingRepositoryMock = Substitute.For<IRepository<Listing>>();

        var filterOptions = new FeedFilterOptions { MatchRole = true };
        
        var userAccount = new Account
        {
            Id = userId,
            MusicianRoles =
            [
                new MusicianRole
                {
                    Name = roleName
                }
            ],
            Name = null,
            PasswordHash = null,
            Email = null,
            DateCreated = default
        };

        var listings = new List<Listing>
        {
            new()
            {
                MusicianSlots =
                [
                    new MusicianSlot
                    {
                        Role = new MusicianRole
                        {
                            Name = differentRole ? "Drummer" : roleName
                        },
                        Status = status
                    }
                ],
                OwnerId = Guid.NewGuid(),
                Name = null,
                Type = ListingType.CollaborativeSong
            }
        };

        authenticationServiceMock.GetUserId().Returns(userId);
        accountServiceMock.GetDetailedAccount(userId).Returns(userAccount);
        listingRepositoryMock.GetAsync(
            Arg.Any<Expression<Func<Listing, bool>>>(),
            Arg.Any<Func<IQueryable<Listing>, IOrderedQueryable<Listing>>>(),
            Arg.Any<string[]>()
        ).Returns(listings);
        musicTasteServiceMock.CompareMusicTasteAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(1);

        var service = new ListingService(
            accountServiceMock,
            authenticationServiceMock,
            musicTasteServiceMock,
            _chatroomServiceMock,
            _genreRepositoryMock,
            _musicianRoleRepositoryMock,
            _musicianSlotRepositoryMock,
            listingRepositoryMock
        );

        // Act
        var result = await service.GetListingsFeedAsync(filterOptions);

        // Assert
        Assert.AreEqual(result.Listings.Count, 0);
    }
}