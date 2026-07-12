using System.Linq.Expressions;
using System.Security.Claims;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class ListingServiceTests
{
    private readonly IChatroomService _chatroomServiceMock = Substitute.For<IChatroomService>();
    private readonly IAuthorizationService _authorizationServiceMock = Substitute.For<IAuthorizationService>();
    private readonly IRepository<Genre> _genreRepositoryMock = Substitute.For<IRepository<Genre>>();
    private readonly IRepository<MusicianRole> _musicianRoleRepositoryMock = Substitute.For<IRepository<MusicianRole>>();
    private readonly IRepository<MusicianSlot> _musicianSlotRepositoryMock = Substitute.For<IRepository<MusicianSlot>>();
    private readonly IValidator<Listing> _validator = Substitute.For<IValidator<Listing>>();

    [Test]
    public async Task GetListingsFeedAsync_WithRoleFilter_FiltersListingsCorrectly()
    {
        var userId = Guid.NewGuid();
        var listings = new List<Listing>
        {
            CreateListing(
                roleName: "Guitarist",
                slotStatus: SlotStatus.Available,
                ownerId: Guid.NewGuid()),
            CreateListing(
                roleName: "Drummer",
                slotStatus: SlotStatus.Filled,
                ownerId: Guid.NewGuid())
        };

        var service = CreateService(userId, ["Guitarist"], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions { MatchRole = true });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
        Assert.That(result.Listings.First().Listing.Id, Is.EqualTo(listings[0].Id));
    }

    [TestCase(SlotStatus.Filled, true)]
    [TestCase(SlotStatus.Filled, false)]
    [TestCase(SlotStatus.Available, true)]
    public async Task GetListingsFeedAsync_WithRoleFilter_FiltersOutIncorrectListings(SlotStatus status, bool differentRole)
    {
        var userId = Guid.NewGuid();
        const string roleName = "Guitarist";
        var listings = new List<Listing>
        {
            CreateListing(
                roleName: differentRole ? "Drummer" : roleName,
                slotStatus: status,
                ownerId: Guid.NewGuid())
        };

        var service = CreateService(userId, [roleName], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions { MatchRole = true });

        Assert.That(result.Listings, Is.Empty);
    }

    [Test]
    public async Task GetListingsFeedAsync_WithRoleFilter_KeepsAvailableMatchingRole()
    {
        var userId = Guid.NewGuid();
        var listings = new List<Listing>
        {
            CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid())
        };

        var service = CreateService(userId, ["Guitarist"], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions { MatchRole = true });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetListingsFeedAsync_WithAnyRole_KeepsListingsWithAvailableSlots()
    {
        var userId = Guid.NewGuid();
        var matching = CreateListing("Drummer", SlotStatus.Available, Guid.NewGuid());
        var noAvailableSlots = CreateListing("Guitarist", SlotStatus.Filled, Guid.NewGuid());
        var listings = new List<Listing> { matching, noAvailableSlots };

        var service = CreateService(userId, ["Any"], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions { MatchRole = true });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
        Assert.That(result.Listings.First().Listing.Id, Is.EqualTo(matching.Id));
    }

    [Test]
    public async Task GetListingsFeedAsync_ExcludeOwn_ExcludesCurrentUserListings()
    {
        var userId = Guid.NewGuid();
        var ownListing = CreateListing("Guitarist", SlotStatus.Available, userId);
        var otherListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid());
        var listings = new List<Listing> { ownListing, otherListing };

        var service = CreateService(userId, ["Guitarist"], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions
        {
            MatchRole = false,
            ExcludeOwn = true
        });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
        Assert.That(result.Listings.First().Listing.Id, Is.EqualTo(otherListing.Id));
    }

    [Test]
    public async Task GetListingsFeedAsync_ListingTypeFilter_ReturnsOnlyMatchingType()
    {
        var userId = Guid.NewGuid();
        var bandListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid(), ListingType.Band);
        var songListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid(), ListingType.CollaborativeSong);
        var listings = new List<Listing> { bandListing, songListing };

        var service = CreateService(userId, [], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions
        {
            MatchRole = false,
            ListingType = ListingType.Band
        });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
        Assert.That(result.Listings.First().Listing.Id, Is.EqualTo(bandListing.Id));
    }

    [Test]
    public async Task GetListingsFeedAsync_GenreFilter_ReturnsOnlyMatchingGenre()
    {
        var userId = Guid.NewGuid();
        var rockListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid(), genreName: "Rock");
        var jazzListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid(), genreName: "Jazz");
        var listings = new List<Listing> { rockListing, jazzListing };

        var service = CreateService(userId, [], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions
        {
            MatchRole = false,
            Genre = "Rock"
        });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
        Assert.That(result.Listings.First().Listing.Id, Is.EqualTo(rockListing.Id));
    }

    [Test]
    public async Task GetListingsFeedAsync_WithPaging_ReturnsPageAfterSimilaritySort()
    {
        var userId = Guid.NewGuid();
        var lowScoreListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid());
        var midScoreListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid());
        var highScoreListing = CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid());
        var listings = new List<Listing> { lowScoreListing, midScoreListing, highScoreListing };

        var service = CreateService(
            userId,
            ["Guitarist"],
            listings,
            similarityScores: new Dictionary<Guid, int>
            {
                [lowScoreListing.OwnerId] = 1,
                [midScoreListing.OwnerId] = 5,
                [highScoreListing.OwnerId] = 10
            });

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions
        {
            MatchRole = true,
            PageNumber = 2,
            PageSize = 1
        });

        Assert.That(result.Listings, Has.Count.EqualTo(1));
        Assert.That(result.Listings.First().Listing.Id, Is.EqualTo(midScoreListing.Id));
        Assert.That(result.Listings.First().SimilarityScore, Is.EqualTo(5));
    }

    [Test]
    public async Task GetListingsFeedAsync_NoMusicianRoles_DoesNotFilterByRole()
    {
        var userId = Guid.NewGuid();
        var listings = new List<Listing>
        {
            CreateListing("Drummer", SlotStatus.Filled, Guid.NewGuid()),
            CreateListing("Guitarist", SlotStatus.Available, Guid.NewGuid())
        };

        var service = CreateService(userId, [], listings);

        var result = await service.GetListingsFeedAsync(new FeedFilterOptions { MatchRole = true });

        Assert.That(result.Listings, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdateListing_UpdatesExistingDescription()
    {
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var listing = new Listing
        {
            Id = listingId,
            Name = "Test listing",
            OwnerId = userId,
            Type = ListingType.Band,
            Description = "Original description",
            MusicianSlots = [],
        };

        var listingRepositoryMock = Substitute.For<IRepository<Listing>>();
        listingRepositoryMock
            .GetOneAsync(
                Arg.Any<Expression<Func<Listing, bool>>>(),
                Arg.Any<string[]>())
            .Returns(listing);

        _validator.ValidateAsync(Arg.Any<Listing>())
            .Returns(new ValidationResult());

        _authorizationServiceMock
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        var authenticationServiceMock = Substitute.For<IAuthenticationService>();
        authenticationServiceMock.GetUserClaims().Returns(new ClaimsPrincipal());

        var service = new ListingService(
            Substitute.For<IAccountService>(),
            authenticationServiceMock,
            _authorizationServiceMock,
            Substitute.For<IMusicTasteService>(),
            _chatroomServiceMock,
            _validator,
            _genreRepositoryMock,
            _musicianRoleRepositoryMock,
            _musicianSlotRepositoryMock,
            listingRepositoryMock);

        var updateDto = new ListingUpdateDto
        {
            Name = listing.Name,
            Type = listing.Type,
            Description = "Updated description",
            MusicianSlots = [],
        };

        await service.UpdateListing(listingId, updateDto);

        Assert.That(listing.Description, Is.EqualTo("Updated description"));
        await listingRepositoryMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task UpdateListing_ClearsExistingDescription()
    {
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var listing = new Listing
        {
            Id = listingId,
            Name = "Test listing",
            OwnerId = userId,
            Type = ListingType.Band,
            Description = "Original description",
            MusicianSlots = [],
        };

        var listingRepositoryMock = Substitute.For<IRepository<Listing>>();
        listingRepositoryMock
            .GetOneAsync(
                Arg.Any<Expression<Func<Listing, bool>>>(),
                Arg.Any<string[]>())
            .Returns(listing);

        _validator.ValidateAsync(Arg.Any<Listing>())
            .Returns(new ValidationResult());

        _authorizationServiceMock
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        var authenticationServiceMock = Substitute.For<IAuthenticationService>();
        authenticationServiceMock.GetUserClaims().Returns(new ClaimsPrincipal());

        var service = new ListingService(
            Substitute.For<IAccountService>(),
            authenticationServiceMock,
            _authorizationServiceMock,
            Substitute.For<IMusicTasteService>(),
            _chatroomServiceMock,
            _validator,
            _genreRepositoryMock,
            _musicianRoleRepositoryMock,
            _musicianSlotRepositoryMock,
            listingRepositoryMock);

        var updateDto = new ListingUpdateDto
        {
            Name = listing.Name,
            Type = listing.Type,
            Description = string.Empty,
            MusicianSlots = [],
        };

        await service.UpdateListing(listingId, updateDto);

        Assert.That(listing.Description, Is.EqualTo(string.Empty));
        await listingRepositoryMock.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task UpdateListing_SkipsDescriptionWhenDtoDescriptionIsNull()
    {
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var listing = new Listing
        {
            Id = listingId,
            Name = "Test listing",
            OwnerId = userId,
            Type = ListingType.Band,
            Description = "Original description",
            MusicianSlots = [],
        };

        var listingRepositoryMock = Substitute.For<IRepository<Listing>>();
        listingRepositoryMock
            .GetOneAsync(
                Arg.Any<Expression<Func<Listing, bool>>>(),
                Arg.Any<string[]>())
            .Returns(listing);

        _validator.ValidateAsync(Arg.Any<Listing>())
            .Returns(new ValidationResult());

        _authorizationServiceMock
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        var authenticationServiceMock = Substitute.For<IAuthenticationService>();
        authenticationServiceMock.GetUserClaims().Returns(new ClaimsPrincipal());

        var service = new ListingService(
            Substitute.For<IAccountService>(),
            authenticationServiceMock,
            _authorizationServiceMock,
            Substitute.For<IMusicTasteService>(),
            _chatroomServiceMock,
            _validator,
            _genreRepositoryMock,
            _musicianRoleRepositoryMock,
            _musicianSlotRepositoryMock,
            listingRepositoryMock);

        var updateDto = new ListingUpdateDto
        {
            Name = listing.Name,
            Type = listing.Type,
            Description = null,
            MusicianSlots = [],
        };

        await service.UpdateListing(listingId, updateDto);

        Assert.That(listing.Description, Is.EqualTo("Original description"));
    }

    private ListingService CreateService(
        Guid userId,
        IEnumerable<string> userRoleNames,
        List<Listing> listings,
        Dictionary<Guid, int>? similarityScores = null)
    {
        var accountServiceMock = Substitute.For<IAccountService>();
        var authenticationServiceMock = Substitute.For<IAuthenticationService>();
        var musicTasteServiceMock = Substitute.For<IMusicTasteService>();
        var listingRepositoryMock = Substitute.For<IRepository<Listing>>();

        var userAccount = new Account
        {
            Id = userId,
            MusicianRoles = userRoleNames.Select(name => new MusicianRole { Name = name }).ToList(),
            Name = "user",
            PasswordHash = "hash",
            Email = "user@example.com",
            DateCreated = default
        };

        authenticationServiceMock.GetUserId().Returns(userId);
        accountServiceMock.GetDetailedAccount(userId).Returns(userAccount);
        SetupListingRepositoryWithFilter(listingRepositoryMock, listings);

        musicTasteServiceMock.CompareMusicTasteAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(callInfo =>
            {
                var ownerId = callInfo.ArgAt<Guid>(1);
                return similarityScores?.GetValueOrDefault(ownerId, 1) ?? 1;
            });

        return new ListingService(
            accountServiceMock,
            authenticationServiceMock,
            _authorizationServiceMock,
            musicTasteServiceMock,
            _chatroomServiceMock,
            _validator,
            _genreRepositoryMock,
            _musicianRoleRepositoryMock,
            _musicianSlotRepositoryMock,
            listingRepositoryMock);
    }

    private static void SetupListingRepositoryWithFilter(
        IRepository<Listing> listingRepositoryMock,
        List<Listing> listings)
    {
        listingRepositoryMock
            .GetAsync(
                Arg.Any<Expression<Func<Listing, bool>>>(),
                Arg.Any<Func<IQueryable<Listing>, IOrderedQueryable<Listing>>>(),
                Arg.Any<string[]>())
            .Returns(callInfo =>
            {
                var filter = callInfo.ArgAt<Expression<Func<Listing, bool>>>(0);
                var orderBy = callInfo.ArgAt<Func<IQueryable<Listing>, IOrderedQueryable<Listing>>?>(1);

                IQueryable<Listing> query = listings.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                if (orderBy != null)
                {
                    query = orderBy(query);
                }

                return Task.FromResult<IEnumerable<Listing>>(query.ToList());
            });
    }

    private static Listing CreateListing(
        string roleName,
        SlotStatus slotStatus,
        Guid ownerId,
        ListingType type = ListingType.Band,
        string? genreName = null)
    {
        return new Listing
        {
            Name = "Test listing",
            OwnerId = ownerId,
            Owner = new Account
            {
                Id = ownerId,
                Name = "Test Owner",
                Email = "owner@example.com",
                PasswordHash = "hash"
            },
            Type = type,
            GenreName = genreName,
            MusicianSlots =
            [
                new MusicianSlot
                {
                    Role = new MusicianRole { Name = roleName },
                    Status = slotStatus
                }
            ]
        };
    }
}