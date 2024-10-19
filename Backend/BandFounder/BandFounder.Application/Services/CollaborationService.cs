using BandFounder.Application.Dtos;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Errors.Api;

namespace BandFounder.Application.Services;

public interface ICollaborationService
{
    Task<MusicProjectListingDto> GetListingAsync(Guid listingId);
    Task<IEnumerable<MusicProjectListing>> GetMusicProjectsAsync();
    Task<IEnumerable<MusicProjectListing>> GetMyMusicProjectsAsync();
    Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto);
    Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? musicProjectListingId = null);
}

public class CollaborationService : ICollaborationService
{
    private readonly IAccountService _accountService;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<MusicianSlot> _musicianSlotRepository;
    private readonly IRepository<MusicProjectListing> _musicProjectListingRepository;
    
    private Guid UserId => _userAuthenticationService.GetUserId();

    public CollaborationService(
        IAccountService accountService,
        IUserAuthenticationService userAuthenticationService,
        IRepository<Genre> genreRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<MusicianSlot> musicianSlotRepository,
        IRepository<MusicProjectListing> musicProjectListingRepository)
    {
        _accountService = accountService;
        _userAuthenticationService = userAuthenticationService;
        _genreRepository = genreRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _musicianSlotRepository = musicianSlotRepository;
        _musicProjectListingRepository = musicProjectListingRepository;
    }

    public async Task<MusicProjectListingDto> GetListingAsync(Guid listingId)
    {
        var projectListing = await _musicProjectListingRepository.GetOneRequiredAsync(listingId, "Id",
            nameof(MusicProjectListing.Owner), nameof(MusicProjectListing.MusicianSlots), "MusicianSlots.Role");
        
        return projectListing.ToDto();
    }
    
    public async Task<IEnumerable<MusicProjectListing>> GetMusicProjectsAsync()
    {
        var projectListings = await _musicProjectListingRepository.GetAsync();

        return projectListings;
    }
    
    public async Task<IEnumerable<MusicProjectListing>> GetMyMusicProjectsAsync()
    {
        var myProjectListings = await _musicProjectListingRepository.GetAsync(project => project.Owner.Id == UserId);

        return myProjectListings;
    }

    public async Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto)
    {
        var userId = _userAuthenticationService.GetUserId();
        await _accountService.GetAccountAsync(userId);

        Genre? projectGenre = null;
        if (dto.GenreName is not null)
        {
            projectGenre = await _genreRepository.GetOrCreateAsync(dto.GenreName);
        }

        var musicProjectListing = new MusicProjectListing
        {
            AccountId = userId,
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

        if (UserId != projectListing.AccountId)
        {
            throw new ForbiddenError("You do not have access to this music project listing");
        }
        
        musicianSlot.Status = slotStatus;
    }
}