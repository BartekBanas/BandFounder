using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAccount([FromBody] RegisterAccountDto dto)
    {
        var token = await _accountService.RegisterAccountAsync(dto);
        
        return Ok(token);
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] LoginDto dto)
    {
        var token = await _accountService.AuthenticateAsync(dto);

        return Ok(token);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllAccounts(string? username, int? pageSize, int? pageNumber)
    {
        IEnumerable<AccountDto> accountDtos;

        if (username is not null)
        {
            var accountDto = await _accountService.GetAccountAsync(username);
            return Ok(accountDto);
        }

        if (pageSize != null && pageNumber != null)
        {
            accountDtos = await _accountService.GetAccountsAsync((int)pageSize, (int)pageNumber);
        }
        else
        {
            accountDtos = await _accountService.GetAccountsAsync();
        }

        return Ok(accountDtos);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var accountDto = await _accountService.GetAccountAsync();
        
        return Ok(accountDto);
    }

    [Authorize]
    [HttpGet("{accountGuid:guid}")]
    public async Task<IActionResult> GetAccount([FromRoute] Guid accountGuid)
    {
        var accountDto = await _accountService.GetAccountAsync(accountGuid);

        return Ok(accountDto);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateAccountDto dto)
    {
        var updatedAccount = await _accountService.UpdateAccountAsync(dto);

        return Ok(updatedAccount);
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount()
    {
        await _accountService.DeleteAccountAsync();

        return Ok();
    }

    [Authorize]
    [HttpPut("roles")]
    public async Task<IActionResult> AddMusicianRole([FromBody] string role)
    {
        await _accountService.AddMusicianRole(role);
        
        return Ok();
    }

    [Authorize]
    [HttpGet("{accountId:guid}/roles")]
    public async Task<IActionResult> AddMusicianRole(Guid? accountId)
    {
        var roles = await _accountService.GetUserMusicianRoles(accountId);

        return Ok(roles.Select(musicianRole => musicianRole.Name));
    }
    
    [Authorize]
    [HttpDelete("roles")]
    public async Task<IActionResult> RemoveMusicianRole([FromBody] string role)
    {
        await _accountService.RemoveMusicianRole(role);
        
        return Ok();
    }

    [Authorize]
    [HttpPost("clearProfile")]
    public async Task<IActionResult> ClearProfile()
    {
        await _accountService.ClearUserMusicProfile();
        
        return Ok();
    }
}