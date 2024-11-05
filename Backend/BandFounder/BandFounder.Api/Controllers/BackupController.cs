using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
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
    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<Genre> _genreRepository;

    public BackupController(IAccountService accountService,
        IRepository<Artist> artistRepository, IRepository<Genre> genreRepository)
    {
        _accountService = accountService;
        _artistRepository = artistRepository;
        _genreRepository = genreRepository;
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
}