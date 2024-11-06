using BandFounder.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api")]
public class ContentController : Controller
{
    private readonly IContentService _contentService;
    private readonly IAccountService _accountService;

    public ContentController(IContentService contentService, IAccountService accountService)
    {
        _contentService = contentService;
        _accountService = accountService;
    }

    [HttpGet("artists")]
    public async Task<IActionResult> GetArtists()
    {
        var artists = await _contentService.GetArtistsAsync();
        
        return Ok(artists);
    }

    [HttpGet("genres")]
    public async Task<IActionResult> GetGenres()
    {
        var artists = await _contentService.GetGenresAsync();
        
        return Ok(artists);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetMusicianRoles()
    {
        var artists = await _contentService.GetMusicianRoles();
        
        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("account/me/artists")]
    public async Task<IActionResult> GetMyArtists()
    {
        var accountDto = await _accountService.GetDetailedAccount();
        var artists = accountDto.Artists.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("account/{accountGuid:guid}/artists")]
    public async Task<IActionResult> GetUsersArtists([FromRoute] Guid accountGuid)
    {
        var accountDto = await _accountService.GetDetailedAccount(accountGuid);
        var artists = accountDto.Artists.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
}