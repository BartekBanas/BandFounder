using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/backup")]
public class BackupController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IListingService _listingService;
    private readonly IHashingService _hashingService;

    public BackupController(IAccountService accountService, IRepository<Artist> artistRepository, 
        IRepository<Genre> genreRepository, IRepository<Account> accountRepository, IHashingService hashingService, 
        IRepository<MusicianRole> musicianRoleRepository, IListingService listingService)
    {
        _accountService = accountService;
        _artistRepository = artistRepository;
        _genreRepository = genreRepository;
        _accountRepository = accountRepository;
        _hashingService = hashingService;
        _musicianRoleRepository = musicianRoleRepository;
        _listingService = listingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBackup()
    {
        var artists = (await _artistRepository.GetAsync()).ToDto();
        
        List<AccountBackup> accounts = [];
        var accountDtos = await _accountService.GetAccountsAsync();
        foreach (var accountDto in accountDtos)
        {
            var account = await _accountService.GetDetailedAccount(Guid.Parse(accountDto.Id));
            var accountBackup = account.ToBackupDto();
            
            var usersListings = await _listingService.GetUserListingsAsync(account.Id);
            var listingDtos = usersListings.Select(listing => new ListingCreateDto
            {
                Name = listing.Name,
                Genre = listing.GenreName,
                Type = listing.Type,
                Description = listing.Description,
                MusicianSlots = listing.MusicianSlots.Select(slot => new MusicianSlotCreateDto { Role = slot.Role.Name }).ToList()
            }).ToList();
            
            accountBackup.Listings = listingDtos;
            
            accounts.Add(accountBackup);
        }
        
        var dto = new BackupDto()
        {
            Accounts = accounts,
            Artists = artists
        };
        
        return Ok(dto);
    }
    
    [HttpPost]
    public async Task<IActionResult> RestoreBackup([FromBody] BackupDto backupDto)
    {
        var artists = await RestoreArtists(backupDto.Artists);
        await RestoreAccounts(backupDto.Accounts, artists);

        await _accountRepository.SaveChangesAsync();

        return Ok();
    }

    private async Task<List<Artist>> RestoreArtists(IEnumerable<ArtistDto> artists)
    {
        List<Artist> result = [];
        
        foreach (var artistDto in artists)
        {
            var newArtist = new Artist
            {
                Id = artistDto.Id,
                Name = artistDto.Name
            };

            foreach (var genreName in artistDto.Genres)
            {
                var genre = await _genreRepository.GetOrCreateAsync(genreName);
            
                newArtist.Genres.Add(genre);
            }
            
            result.Add(newArtist);
        }
        
        return result;
    }

    private async Task RestoreAccounts(IEnumerable<AccountBackup> accounts, List<Artist> artists)
    {
        foreach (var accountBackup in accounts)
        {
            var id = Guid.NewGuid();
            
            var account = new Account
            {
                Id = id,
                Name = accountBackup.Name,
                Email = accountBackup.Email,
                SpotifyTokens = accountBackup.SpotifyTokens is not null
                    ? new SpotifyTokens
                    {
                        AccessToken = accountBackup.SpotifyTokens.AccessToken,
                        RefreshToken = accountBackup.SpotifyTokens.RefreshToken,
                        ExpirationDate = accountBackup.SpotifyTokens.ExpirationDate,
                        AccountId = id,
                    }
                    : null,
                PasswordHash = _hashingService.HashPassword(accountBackup.Name),
                DateCreated = DateTime.UtcNow
            };
            
            account.Artists.AddRange(artists);
            
            foreach (var musicianRole in accountBackup.MusicianRoles)
            {
                var role = await _musicianRoleRepository.GetOrCreateAsync(musicianRole);
                
                account.MusicianRoles.Add(role);
            }
            
            await _accountRepository.CreateAsync(account);
            
            await RestoreUsersListings(accountBackup.Listings, account.Id);
        }
    }
    
    private async Task RestoreUsersListings(IEnumerable<ListingCreateDto> listings, Guid accountId)
    {
        foreach (var listingDto in listings)
        {
            await _listingService.CreateListingAsync(listingDto, accountId);
        }
    }
}