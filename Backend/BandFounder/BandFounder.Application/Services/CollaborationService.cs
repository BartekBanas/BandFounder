using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Error;
using BandFounder.Domain;
using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services;

public interface ICollaborationService
{
    Task<MusicProjectListingDto> GetListingAsync(Guid listingId);
    Task<IEnumerable<MusicProjectListingDto>> GetMusicProjectsAsync();
    Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions);
    Task<IEnumerable<MusicProjectListingDto>> GetMyMusicProjectsAsync();
    Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto);
    Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? musicProjectListingId = null);
    Task Contact(Guid musicProjectListingId);
    Task DeleteListing(Guid listingId);
}

public class CollaborationService : ICollaborationService
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IMusicTasteComparisonService _musicTasteService;
    private readonly IChatroomService _chatroomService;
    
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<MusicianSlot> _musicianSlotRepository;
    private readonly IRepository<MusicProjectListing> _musicProjectListingRepository;
    
    private Guid UserId => _authenticationService.GetUserId();

    public CollaborationService(
        IAccountService accountService,
        IAuthenticationService authenticationService,
        IMusicTasteComparisonService musicTasteService,
        IChatroomService chatroomService,
        IRepository<Genre> genreRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<MusicianSlot> musicianSlotRepository,
        IRepository<MusicProjectListing> musicProjectListingRepository)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _musicTasteService = musicTasteService;
        _chatroomService = chatroomService;
        _genreRepository = genreRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _musicianSlotRepository = musicianSlotRepository;
        _musicProjectListingRepository = musicProjectListingRepository;
    }

    public async Task<MusicProjectListingDto> GetListingAsync(Guid listingId)
    {
        var projectListing = await _musicProjectListingRepository.GetOneRequiredAsync(
            filter: listing => listing.Id == listingId,
            includeProperties:
            [nameof(MusicProjectListing.Owner), nameof(MusicProjectListing.MusicianSlots), "MusicianSlots.Role"]);
        
        return projectListing.ToDto();
    }
    
    public async Task<IEnumerable<MusicProjectListingDto>> GetMusicProjectsAsync()
    {
        var projectListings = await _musicProjectListingRepository.GetAsync(includeProperties:
            [nameof(MusicProjectListing.Owner), nameof(MusicProjectListing.MusicianSlots), "MusicianSlots.Role"]);

        return projectListings.ToDto();
    }
    
    public async Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions)
    {
        var userId = _authenticationService.GetUserId();
        var userAccount = await _accountService.GetDetailedAccount(userId);
        
        var projectListings = await _musicProjectListingRepository.GetAsync(includeProperties:
            [nameof(MusicProjectListing.Owner), nameof(MusicProjectListing.MusicianSlots), "MusicianSlots.Role"]);

        var listingsList = projectListings.ToList();
        FilterListings(userAccount, listingsList, filterOptions);
        
        var projectListingsWithScores = new List<ListingWithScore>();

        foreach (var projectListing in listingsList)
        {
            var similarityScore = await _musicTasteService.CompareMusicTasteAsync(userId, projectListing.OwnerId);
            projectListingsWithScores.Add(new ListingWithScore()
            {
                Listing = projectListing.ToDto(),
                SimilarityScore = similarityScore
            });
        }
        
        projectListingsWithScores = projectListingsWithScores
            .OrderByDescending(listing => listing.SimilarityScore)
            .ToList();
        
        return new ListingsFeedDto
        {
            Listings = projectListingsWithScores
        };
    }
    
    public async Task<IEnumerable<MusicProjectListingDto>> GetMyMusicProjectsAsync()
    {
        var myProjectListings = await _musicProjectListingRepository.GetAsync(
            filter: listing => listing.OwnerId == UserId,
            includeProperties: [nameof(MusicProjectListing.Owner), nameof(MusicProjectListing.MusicianSlots), "MusicianSlots.Role"]);

        return myProjectListings.ToDto();
    }

    public async Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto)
    {
        var userId = _authenticationService.GetUserId();
        await _accountService.GetAccountAsync(userId);

        Genre? projectGenre = null;
        if (dto.GenreName is not null)
        {
            projectGenre = await _genreRepository.GetOrCreateAsync(dto.GenreName);
        }

        var musicProjectListing = new MusicProjectListing
        {
            OwnerId = userId,
            Name = dto.Name,
            Genre = projectGenre,
            Type = dto.Type,
            Description = dto.Description
        };

        foreach (var slotDto in dto.MusicianSlots)
        {
            var role = await _musicianRoleRepository.GetOrCreateAsync(slotDto.RoleName);

            var musicianSlot = new MusicianSlot
            {
                Role = role,
                Status = slotDto.Status,
                Listing = musicProjectListing
            };

            musicProjectListing.MusicianSlots.Add(musicianSlot);
        }

        await _musicProjectListingRepository.CreateAsync(musicProjectListing);
        await _musicianRoleRepository.SaveChangesAsync();

        return musicProjectListing;
    }

    public async Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? musicProjectListingId = null)
    {
        var musicianSlot = await _musicianSlotRepository.GetOneRequiredAsync(slotId);
        var projectListing = await _musicProjectListingRepository.GetOneRequiredAsync
            (listing => listing.Id == musicianSlot.ListingId);

        if (UserId != projectListing.OwnerId)
        {
            throw new ForbiddenError("You do not have access to this music project listing");
        }
        
        musicianSlot.Status = slotStatus;
        
        await _musicianSlotRepository.SaveChangesAsync();
    }

    public async Task Contact(Guid musicProjectListingId)
    {
        var listing = await _musicProjectListingRepository.GetOneRequiredAsync(musicProjectListingId);
        
        var chatroomCreateDto = new ChatroomCreateDto()
        {
            ChatRoomType = ChatRoomType.Direct,
            InvitedAccountId = listing.OwnerId
        };

        await _chatroomService.CreateChatroom(chatroomCreateDto);
    }

    public async Task DeleteListing(Guid listingId)
    {
        var listing = await _musicProjectListingRepository.GetOneRequiredAsync(listingId);

        if (listing.OwnerId != UserId)
        {
            throw new ForbiddenError("You may only delete your own listings.");
        }
        
        await _musicProjectListingRepository.DeleteOneAsync(listing.Id);
        await _musicProjectListingRepository.SaveChangesAsync();
    }

    private void FilterListings(Account account, List<MusicProjectListing> listings, FeedFilterOptions filterOptions)
    {
        if (filterOptions.ExcludeOwn)
        {
            listings.RemoveAll(listing => listing.OwnerId == account.Id);
        }
        
        if (filterOptions.MatchRole)
        {
            listings.RemoveAll(listing => 
                !listing.MusicianSlots.Any(slot => 
                    slot.Status == SlotStatus.Available &&
                    account.MusicianRoles.Any(role => role.Id == slot.Role.Id)
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
    }
}