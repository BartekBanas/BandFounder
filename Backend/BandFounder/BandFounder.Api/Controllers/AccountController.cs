using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using BandFounder.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IMusicTasteService _musicTasteService;

    public AccountController(IAccountService accountService, IMusicTasteService musicTasteService)
    {
        _accountService = accountService;
        _musicTasteService = musicTasteService;
    }

    [HttpPost]
    [EnableRateLimiting("IpRateLimiting")]
    public async Task<IActionResult> RegisterAccount([FromBody] RegisterAccountDto dto)
    {
        var token = await _accountService.RegisterAccountAsync(dto);
        
        return Ok(token);
    }

    [HttpPost("authenticate")]
    [EnableRateLimiting("IpRateLimiting")]
    public async Task<IActionResult> Authenticate([FromBody] LoginDto dto)
    {
        var token = await _accountService.AuthenticateAsync(dto);

        return Ok(token);
    }

    [HttpPost("password-reset/request")]
    [EnableRateLimiting("IpRateLimiting")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
    {
        await _accountService.RequestPasswordResetAsync(dto);

        return Ok(new
        {
            message = "If an account with that email exists, a password reset link has been sent."
        });
    }

    [HttpPost("password-reset/complete")]
    [EnableRateLimiting("IpRateLimiting")]
    public async Task<IActionResult> CompletePasswordReset([FromBody] CompletePasswordResetDto dto)
    {
        await _accountService.CompletePasswordResetAsync(dto);

        return Ok(new
        {
            message = "Password has been reset successfully."
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAccounts([FromQuery] AccountFilters filters)
    {
        var accounts = await _accountService.GetAccountsAsync(filters);
        
        var accountDtos = accounts.ToDto();
        
        if (filters.Username is not null)
        {
            return Ok(accountDtos.FirstOrDefault() ?? throw new ItemNotFoundException("Account not found."));
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
    [HttpGet("{accountId:guid}/commonTaste")]
    public async Task<IActionResult> GetCommonArtistsAndGenres(Guid accountId)
    {
        var responseDto = await _musicTasteService.GetCommonArtistsAndGenresAsync(accountId);

        return Ok(responseDto);
    }

    [Authorize]
    [HttpPatch("me")]
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
        const long maxFileSize = 10 * 1024 * 1024;
        
        if (file.Length == 0)
        {
            return BadRequest("Provide valid file");
        }
        
        if (file.Length > maxFileSize)
        {
            return BadRequest("File size cannot exceed 10 MB");
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
}