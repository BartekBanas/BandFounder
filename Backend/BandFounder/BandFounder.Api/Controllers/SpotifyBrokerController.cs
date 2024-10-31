using BandFounder.Application.Services;
using BandFounder.Application.Services.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Authorize]
[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly ISpotifyContentManager _spotifyContentManager;
    private readonly ISpotifyCredentialsService _spotifyCredentialsService;
    
    private readonly IAuthenticationService _authenticationService;
    
    public SpotifyBrokerController(
        ISpotifyContentRetriever spotifyContentRetriever,
        ISpotifyContentManager spotifyContentManager,
        ISpotifyCredentialsService spotifyCredentialsService, 
        IAuthenticationService authenticationService)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _spotifyContentManager = spotifyContentManager;
        _spotifyCredentialsService = spotifyCredentialsService;
        _authenticationService = authenticationService;
    }
    
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeSpotifyAccount([FromBody] SpotifyAuthorizationDto dto)
    {
        var userId = _authenticationService.GetUserId();
        
        await _spotifyCredentialsService.CreateSpotifyCredentials(dto, userId);

        return Ok();
    }
    
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var userId = _authenticationService.GetUserId();

        var credentialsDto = await _spotifyCredentialsService.GetSpotifyCredentials(userId);

        return Ok(credentialsDto);
    }
    
    [HttpGet("artists/top")]
    public async Task<IActionResult> GetSpotifyUsersTopArtists()
    {
        var userId = _authenticationService.GetUserId();

        var artists = await _spotifyContentRetriever.GetTopArtistsAsync(userId);

        return Ok(artists);
    }
    
    [HttpGet("artists/followed")]
    public async Task<IActionResult> GetSpotifyUsersFollowedArtists()
    {
        var userId = _authenticationService.GetUserId();

        var artists = await _spotifyContentRetriever.GetFollowedArtistsAsync(userId);

        return Ok(artists);
    }
    
    [HttpPost("artists")]
    public async Task<IActionResult> DownloadSpotifyArtists()
    {
        var artists = await _spotifyContentManager.SaveRelevantArtists();

        return Ok(artists);
    }
    
    [HttpGet("genres/waged")]
    public async Task<IActionResult> GetWagedGenres()
    {
        var wagedGenres = await _spotifyContentManager.GetWagedGenres();

        return Ok(wagedGenres);
    }
}