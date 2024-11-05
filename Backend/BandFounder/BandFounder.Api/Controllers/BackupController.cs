using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Services;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
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
    private readonly IHashingService _hashingService;

    public BackupController(IAccountService accountService, IRepository<Artist> artistRepository, 
        IRepository<Genre> genreRepository, IRepository<Account> accountRepository, IHashingService hashingService, IRepository<MusicianRole> musicianRoleRepository)
    {
        _accountService = accountService;
        _artistRepository = artistRepository;
        _genreRepository = genreRepository;
        _accountRepository = accountRepository;
        _hashingService = hashingService;
        _musicianRoleRepository = musicianRoleRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetBackup()
    {
        var genres = (await _genreRepository.GetAsync()).Select(genre => genre.Name).ToList();
        
        var artists = (await _artistRepository.GetAsync()).ToDto();
        
        List<AccountDetailedDto> accounts = [];
        var accountDtos = await _accountService.GetAccountsAsync();
        foreach (var accountDto in accountDtos)
        {
            var account = await _accountService.GetDetailedAccount(Guid.Parse(accountDto.Id));
            accounts.Add(account.ToDetailedDto());
        }
        
        var dto = new BackupDto()
        {
            Accounts = accounts,
            Artists = artists,
            Genres = genres
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

    private async Task RestoreAccounts(IEnumerable<AccountDetailedDto> accounts, List<Artist> artists)
    {
        foreach (var accountDto in accounts)
        {
            var id = Guid.NewGuid();
            
            var account = new Account
            {
                Id = id,
                Name = accountDto.Name,
                Email = accountDto.Email,
                SpotifyTokens = accountDto.SpotifyTokens is not null
                    ? new SpotifyTokens
                    {
                        AccessToken = accountDto.SpotifyTokens.AccessToken,
                        RefreshToken = accountDto.SpotifyTokens.RefreshToken,
                        ExpirationDate = accountDto.SpotifyTokens.ExpirationDate,
                        AccountId = id,
                    }
                    : null,
                PasswordHash = _hashingService.HashPassword(accountDto.Name),
                DateCreated = DateTime.UtcNow
            };
            
            account.Artists.AddRange(artists);
            
            foreach (var musicianRole in accountDto.MusicianRoles)
            {
                var role = await _musicianRoleRepository.GetOrCreateAsync(musicianRole);
                
                account.MusicianRoles.Add(role);
            }
            
            await _accountRepository.CreateAsync(account);
        }
    }
}