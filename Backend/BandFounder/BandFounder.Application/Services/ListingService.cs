using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Error;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;

namespace BandFounder.Application.Services;

public interface IListingService
{
    Task<Listing?> GetListingAsync(Guid listingId);
    Task<IEnumerable<Listing>> GetListingsAsync();
    Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions);
    Task<IEnumerable<ListingDto>> GetMyListingAsync();
    Task<ArtistsAndGenresDto> GetCommonArtistsAndGenresWithListingsAsync(Guid listingId, Guid? accountId = null);
    Task<Listing> CreateListingAsync(ListingCreateDto dto);
    Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? listingId = null);
    Task ContactOwner(Guid listingId);
    Task DeleteListing(Guid listingId);
}

public class ListingService : IListingService
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IMusicTasteService _musicTasteService;
    private readonly IChatroomService _chatroomService;
    
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<MusicianSlot> _musicianSlotRepository;
    private readonly IRepository<Listing> _listingRepository;
    
    private Guid UserId => _authenticationService.GetUserId();

    public ListingService(
        IAccountService accountService,
        IAuthenticationService authenticationService,
        IMusicTasteService musicTasteService,
        IChatroomService chatroomService,
        IRepository<Genre> genreRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<MusicianSlot> musicianSlotRepository,
        IRepository<Listing> listingRepository)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _musicTasteService = musicTasteService;
        _chatroomService = chatroomService;
        _genreRepository = genreRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _musicianSlotRepository = musicianSlotRepository;
        _listingRepository = listingRepository;
    }

    public async Task<Listing?> GetListingAsync(Guid listingId)
    {
        var listing = await _listingRepository.GetOneAsync(
            filter: listing => listing.Id == listingId,
            includeProperties:
            [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);
        
        return listing;
    }
    
    public async Task<IEnumerable<Listing>> GetListingsAsync()
    {
        var listings = await _listingRepository.GetAsync(includeProperties:
            [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);

        return listings;
    }
    
    public async Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions)
    {
        var userId = _authenticationService.GetUserId();
        var userAccount = await _accountService.GetDetailedAccount(userId);
        
        var listings = await _listingRepository.GetAsync(includeProperties:
            [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);

        var listingsList = listings.ToList();
        FilterListings(userAccount, listingsList, filterOptions);
        
        var listingsWithScores = new List<ListingWithScore>();

        foreach (var listing in listingsList)
        {
            var similarityScore = await _musicTasteService.CompareMusicTasteAsync(userId, listing.OwnerId);
            listingsWithScores.Add(new ListingWithScore()
            {
                Listing = listing.ToDto(),
                SimilarityScore = similarityScore
            });
        }
        
        listingsWithScores = listingsWithScores
            .OrderByDescending(listing => listing.SimilarityScore)
            .ToList();
        
        return new ListingsFeedDto
        {
            Listings = listingsWithScores
        };
    }
    
    public async Task<IEnumerable<ListingDto>> GetMyListingAsync()
    {
        var myListings = await _listingRepository.GetAsync(
            filter: listing => listing.OwnerId == UserId,
            includeProperties: [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);

        return myListings.ToDto();
    }

    public async Task<ArtistsAndGenresDto> GetCommonArtistsAndGenresWithListingsAsync(Guid listingId, Guid? accountId = null)
    {
        var userId = accountId ?? UserId;
        
        var listing = await _listingRepository.GetOneRequiredAsync(listingId);
        
        var commonArtists = await _musicTasteService.GetCommonArtists(userId, listing.OwnerId);
        var commonGenres = await _musicTasteService.GetCommonGenres(userId, listing.OwnerId);
        
        return new ArtistsAndGenresDto(commonArtists, commonGenres);
    }

    public async Task<Listing> CreateListingAsync(ListingCreateDto dto)
    {
        var userId = _authenticationService.GetUserId();
        await _accountService.GetAccountAsync(userId);

        Genre? projectGenre = null;
        if (dto.Genre is not null)
        {
            projectGenre = await _genreRepository.GetOrCreateAsync(dto.Genre);
        }

        var listing = new Listing
        {
            OwnerId = userId,
            Name = dto.Name,
            Genre = projectGenre,
            Type = dto.Type,
            Description = dto.Description
        };

        foreach (var slotDto in dto.MusicianSlots)
        {
            var role = await _musicianRoleRepository.GetOrCreateAsync(slotDto.Role);

            var musicianSlot = new MusicianSlot
            {
                Role = role,
                Status = slotDto.Status,
                Listing = listing
            };

            listing.MusicianSlots.Add(musicianSlot);
        }

        await _listingRepository.CreateAsync(listing);
        await _musicianRoleRepository.SaveChangesAsync();

        return listing;
    }

    public async Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? listingId = null)
    {
        var musicianSlot = await _musicianSlotRepository.GetOneRequiredAsync(slotId);
        var listing = await _listingRepository.GetOneRequiredAsync
            (listing => listing.Id == musicianSlot.ListingId);

        if (UserId != listing.OwnerId)
        {
            throw new ForbiddenError("You do not have access to this music project listing");
        }
        
        musicianSlot.Status = slotStatus;
        
        await _musicianSlotRepository.SaveChangesAsync();
    }

    public async Task ContactOwner(Guid listingId)
    {
        var listing = await _listingRepository.GetOneRequiredAsync(listingId);
        
        var chatroomCreateDto = new ChatroomCreateDto()
        {
            ChatRoomType = ChatRoomType.Direct,
            InvitedAccountId = listing.OwnerId
        };

        await _chatroomService.CreateChatroom(chatroomCreateDto);
    }

    public async Task DeleteListing(Guid listingId)
    {
        var listing = await _listingRepository.GetOneRequiredAsync(listingId);

        if (listing.OwnerId != UserId)
        {
            throw new ForbiddenError("You may only delete your own listings.");
        }
        
        await _listingRepository.DeleteOneAsync(listing.Id);
        await _listingRepository.SaveChangesAsync();
    }

    private void FilterListings(Account account, List<Listing> listings, FeedFilterOptions filterOptions)
    {
        if (filterOptions.ExcludeOwn)
        {
            listings.RemoveAll(listing => listing.OwnerId == account.Id);
        }
        
        if (filterOptions.MatchRole && account.MusicianRoles.Count > 0)
        {
            listings.RemoveAll(listing => 
                !listing.MusicianSlots.Any(slot => 
                    slot.Status == SlotStatus.Available &&
                    account.MusicianRoles.Any(role => role.Name == "Any" || role.Name == slot.Role.Name)
                )
            );
        }

        if (filterOptions.ListingType is not null)
        {
            listings.RemoveAll(listing => listing.Type != filterOptions.ListingType);
        }

        if (filterOptions.Genre is not null)
        {
            listings.RemoveAll(listing => listing.GenreName != filterOptions.Genre);
        }
        
        if (filterOptions.FromLatest)
        {
            listings.Sort((x, y) => y.DateCreated.CompareTo(x.DateCreated));
        }
    }
}