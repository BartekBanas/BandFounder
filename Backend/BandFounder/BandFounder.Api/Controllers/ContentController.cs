using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api")]
public class ContentController(
    IContentService contentService,
    IAccountService accountService,
    ISpotifyContentRetriever spotifyContentRetriever,
    IAuthenticationService authenticationService)
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
    [HttpGet("account/me/artists")]
    public async Task<IActionResult> GetMyArtists()
    {
        var accountDto = await accountService.GetDetailedAccount();
        var artists = accountDto.Artists.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("account/{accountId:guid}/artists")]
    public async Task<IActionResult> GetUsersArtists([FromRoute] Guid accountId)
    {
        var accountDto = await accountService.GetDetailedAccount(accountId);
        var artists = accountDto.Artists.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("account/me/artists/top")]
    public async Task<IActionResult> GetMyTopArtists()
    {
        var userId = authenticationService.GetUserId();
        var artistDtoList = await spotifyContentRetriever.GetTopArtistsAsync(userId, 10);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("account/{accountId:guid}/artists/top")]
    public async Task<IActionResult> GetUsersTopArtists([FromRoute] Guid accountId)
    {
        var artistDtoList = await spotifyContentRetriever.GetTopArtistsAsync(accountId, 10);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
}