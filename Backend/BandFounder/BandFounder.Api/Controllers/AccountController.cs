using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;

    public AccountController(IAccountService accountService, IAuthenticationService authenticationService)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
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
    public async Task<IActionResult> GetAllAccounts([FromQuery] int? pageSize, [FromQuery] int? pageNumber)
    {
        IEnumerable<AccountDto> accountDtos;

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
    public Task<IActionResult> Me()
    {
        var claims = User.Claims;

        var claimsInfo = claims.ToDictionary(claim => claim.Type, claim => claim.Value);

        return Task.FromResult<IActionResult>(Ok(claimsInfo));
    }

    [Authorize]
    [HttpGet("{accountGuid:guid}")]
    public async Task<IActionResult> GetAccount([FromRoute] Guid accountGuid)
    {
        var user = await _accountService.GetAccountAsync(accountGuid);

        return Ok(user);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateAccountDto dto)
    {
        var userId = _authenticationService.GetUserId();

        var updatedAccount = await _accountService.UpdateAccountAsync(dto, userId);

        return Ok(updatedAccount);
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var userId = _authenticationService.GetUserId();

        await _accountService.DeleteAccountAsync(userId);

        return Ok();
    }
}