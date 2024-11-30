using BandFounder.Application.Dtos;
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
    private readonly IPasswordResetService _passwordResetService;

    public AccountController(IAccountService accountService, IPasswordResetService passwordResetService)
    {
        _accountService = accountService;
        _passwordResetService = passwordResetService;
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
    public async Task<IActionResult> GetAccounts([FromQuery] AccountFilters filters)
    {
        var accounts = await _accountService.GetAccountsAsync(filters);
        
        if (!accounts.Any())
        {
            return NotFound();
        }
        
        var accountDtos = accounts.ToDto();
        
        if (filters.Username is not null)
        {
            return Ok(accountDtos.First());
        }

        return Ok(accountDtos);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var accountDto = (await _accountService.GetAccountAsync()).ToDto();
        
        return Ok(accountDto);
    }

    [Authorize]
    [HttpGet("{accountGuid:guid}")]
    public async Task<IActionResult> GetAccount([FromRoute] Guid accountGuid)
    {
        var accountDto = (await _accountService.GetAccountAsync(accountGuid)).ToDto();

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
    
    [HttpPut("{accountId:guid}/profile-picture")]
    [Authorize]
    public async Task<IActionResult> UpdateProfilePicture(Guid accountId, IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("Provide valid file");
        }

        await _accountService.UpdateProfilePicture(accountId, file);

        return NoContent();
    }
    
    [HttpGet("{accountId:guid}/profile-picture")]
    public async Task<IActionResult> GetProfilePicture(Guid accountId)
    {
        var profilePicture = await _accountService.GetProfilePictureAsync(accountId);

        return File(profilePicture.ImageData, profilePicture.MimeType);
    }

    [Authorize]
    [HttpPost("clearProfile")]
    public async Task<IActionResult> ClearProfile()
    {
        await _accountService.ClearUserMusicProfile();
        
        return Ok();
    }
    
    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
    {
        await _passwordResetService.SendResetPasswordEmailAsync(dto);
        
        return Ok();
    }
    
    [HttpPost("password-reset")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto resetDto)
    {
        await _passwordResetService.ResetPasswordAsync(resetDto);
        
        return Ok();
    }
}