using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Backup;
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
    public async Task<IActionResult> GetBackup([FromQuery] bool? profilePictures = false)
    {
        var artists = (await _artistRepository.GetAsync()).ToBackupDto();
        
        List<AccountBackup> accountsBackup = [];
        var accounts = await _accountService.GetAccountsAsync();
        foreach (var account in accounts)
        {
            var propertiesToBackup = new List<string>
            {
                nameof(Account.Artists), "Artists.Genres", nameof(Account.Chatrooms), 
                nameof(Account.MusicianRoles), nameof(Account.SpotifyTokens)
            };
            
            if (profilePictures is true)
            {
                propertiesToBackup.Add(nameof(Account.ProfilePicture));
            }
            
            var detailedAccount = await _accountService.GetDetailedAccount(
                account.Id, includeProperties: propertiesToBackup.ToArray());
            
            var accountBackup = detailedAccount.ToBackupDto();
            
            var usersListings = await _listingService.GetUserListingsAsync(detailedAccount.Id);
            var listingDtos = usersListings.Select(listing => new ListingCreateDto
            {
                Name = listing.Name,
                Genre = listing.GenreName,
                Type = listing.Type,
                Description = listing.Description,
                MusicianSlots = listing.MusicianSlots.Select(slot => new MusicianSlotCreateDto { Role = slot.Role.Name }).ToList()
            }).ToList();
            
            accountBackup.Listings = listingDtos;
            
            accountsBackup.Add(accountBackup);
        }
        
        var dto = new BackupDto()
        {
            Accounts = accountsBackup,
            Artists = artists
        };
        
        return Ok(dto);
    }
    
    [HttpPost]
    public async Task<IActionResult> RestoreBackup([FromBody] BackupDto backupDto)
    {
        await RestoreArtists(backupDto.Artists);
        await RestoreAccounts(backupDto.Accounts);

        await _accountRepository.SaveChangesAsync();

        return Ok();
    }

    private async Task RestoreArtists(IEnumerable<ArtistBackup> artists)
    {
        foreach (var artistDto in artists)
        {
            await _artistRepository.GetOrCreateAsync(
                _genreRepository, artistDto.Name, artistDto.Genres, artistDto.Popularity, artistDto.Id);
        }
    }

    private async Task RestoreAccounts(IEnumerable<AccountBackup> accounts)
    {
        foreach (var accountBackup in accounts)
        {
            var existingAccount = await _accountRepository.GetOneAsync(
                a => a.Name == accountBackup.Name || a.Email == accountBackup.Email);
            
            if (existingAccount != null)
            {
                continue;
            }
            
            var id = Guid.NewGuid();
            
            var account = new Account
            {
                Id = id,
                Name = accountBackup.Name,
                Email = accountBackup.Email,
                ProfilePicture = accountBackup.ProfilePicture is not null
                    ? new ProfilePicture
                    {
                        AccountId = id,
                        MimeType = accountBackup.ProfilePicture.MimeType,
                        ImageData = Convert.FromBase64String(accountBackup.ProfilePicture.ImageDataBase64)
                    }
                    : null,
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
            
            // Restoring account's artists
            var artists = await _artistRepository.GetOrCreateAsync(accountBackup.Artists);
            account.Artists.AddRange(artists);
            
            // Restoring account's musician roles
            foreach (var musicianRole in accountBackup.MusicianRoles)
            {
                var role = await _musicianRoleRepository.GetOrCreateAsync(musicianRole);
                
                account.MusicianRoles.Add(role);
            }
            
            await _accountRepository.CreateAsync(account);
            
            if (accountBackup.Listings != null) await RestoreUsersListings(accountBackup.Listings, account.Id);
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