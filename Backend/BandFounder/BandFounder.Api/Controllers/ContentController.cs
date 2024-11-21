using BandFounder.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api")]
public class ContentController(
    IContentService contentService,
    IAccountService accountService,
    IListingService listingService)
    : Controller
{
    [HttpGet("artists")]
    public async Task<IActionResult> GetArtists()
    {
        var artists = await contentService.GetArtistsAsync();
        
        return Ok(artists);
    }

    [HttpGet("genres")]
    public async Task<IActionResult> GetGenres()
    {
        var genres = await contentService.GetGenresAsync();
        
        return Ok(genres);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetMusicianRoles()
    {
        var musicianRoles = await contentService.GetMusicianRoles();
        
        return Ok(musicianRoles);
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists")]
    public async Task<IActionResult> GetUsersArtists([FromRoute] Guid accountId)
    {
        var accountDto = await accountService.GetDetailedAccount(accountId);
        var artists = accountDto.Artists.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpPost("accounts/{accountId:guid}/artists")]
    public async Task<IActionResult> AddArtistToAccount([FromRoute] Guid accountId, [FromBody] string artistName)
    {
        await accountService.AddArtist(accountId, artistName);

        return Ok();
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/genres")]
    public async Task<IActionResult> GetUsersGenres([FromRoute] Guid accountId)
    {
        var wagedGenres = await contentService.GetWagedGenres(accountId);
        var genres = wagedGenres.Select(artist => artist.Key).ToList();

        return Ok(genres);
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/listings")]
    public async Task<IActionResult> GetUsersListings([FromRoute] Guid accountId)
    {
        var listings = await listingService.GetUserListingsAsync(accountId);
        
        return Ok(listings);
    }
}