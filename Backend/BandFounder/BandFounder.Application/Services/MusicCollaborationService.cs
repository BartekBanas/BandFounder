using BandFounder.Application.Dtos;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Entities.Spotify;

namespace BandFounder.Application.Services;

public interface IMusicCollaborationService
{
    Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto);
}

public class MusicCollaborationService : IMusicCollaborationService
{
    private readonly IAccountService _accountService;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<MusicProjectListing> _musicProjectListingRepository;

    public MusicCollaborationService(
        IAccountService accountService,
        IUserAuthenticationService userAuthenticationService, 
        IRepository<Genre> genreRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<MusicProjectListing> musicProjectListingRepository)
    {
        _accountService = accountService;
        _userAuthenticationService = userAuthenticationService;
        _genreRepository = genreRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _musicProjectListingRepository = musicProjectListingRepository;
    }

    public async Task<MusicProjectListing> CreateMusicProjectListingAsync(MusicProjectListingCreateDto dto)
    {
        var accountId = _userAuthenticationService.GetUserId();
        await _accountService.GetAccountAsync(accountId);

        if (dto.GenreName is not null)
        {
            await _genreRepository.TryAdd(dto.GenreName);
        }

        var musicProjectListing = new MusicProjectListing
        {
            AccountId = accountId,
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