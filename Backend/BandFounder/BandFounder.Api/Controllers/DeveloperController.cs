using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/developer")]
public class DeveloperController : Controller
{
    private readonly IAccountService _accountService;

    public DeveloperController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("account")]
    public async Task<IActionResult> GetArtists()
    {
        List<AccountDetailedDto> accounts = [];
        var accountDtos = await _accountService.GetAccountsAsync();

        foreach (var accountDto in accountDtos)
        {
            var account = await _accountService.GetDetailedAccount(Guid.Parse(accountDto.Id));
            accounts.Add(account.ToDetailedDto());
        }
        
        return Ok(accounts);
    }
}