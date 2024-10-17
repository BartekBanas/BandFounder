using BandFounder.Application.Dtos;
using BandFounder.Domain;
using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services;

public interface IMusicCollaborationService
{
    Task<IEnumerable<MusicProjectListing>> GetMusicProjectsAsync();
    Task<IEnumerable<MusicProjectListing>> GetMyMusicProjectsAsync();
    Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto);
}

public class MusicCollaborationService : IMusicCollaborationService
{
    private readonly IAccountService _accountService;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<MusicianSlot> _musicianSlotRepository;
    private readonly IRepository<MusicProjectListing> _musicProjectListingRepository;

    public MusicCollaborationService(
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
    
    public async Task<IEnumerable<MusicProjectListing>> GetMusicProjectsAsync()
    {
        var projectListings = await _musicProjectListingRepository.GetAsync();

        return projectListings;
    }
    
    public async Task<IEnumerable<MusicProjectListing>> GetMyMusicProjectsAsync()
    {
        var userId = _userAuthenticationService.GetUserId();

        var myProjectListings = await _musicProjectListingRepository.GetAsync(project => project.Owner.Id == userId);

        return myProjectListings;
    }

    public async Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto)
    {
        var userId = _userAuthenticationService.GetUserId();
        await _accountService.GetAccountAsync(userId);

        if (dto.GenreName is not null)
        {
            await _genreRepository.TryAdd(dto.GenreName);
        }

        var musicProjectListing = new MusicProjectListing
        {
            AccountId = userId,
            GenreName = dto.GenreName,
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
}