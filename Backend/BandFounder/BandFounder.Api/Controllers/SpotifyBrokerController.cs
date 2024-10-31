using BandFounder.Application.Services;
using BandFounder.Application.Services.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly ISpotifyContentManager _spotifyContentManager;
    private readonly ISpotifyTokenService _spotifyTokenService;
    
    private readonly IAuthenticationService _authenticationService;
    
    public SpotifyBrokerController(
        ISpotifyContentRetriever spotifyContentRetriever,
        ISpotifyContentManager spotifyContentManager,
        ISpotifyTokenService spotifyTokenService, 
        IAuthenticationService authenticationService)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _spotifyContentManager = spotifyContentManager;
        _spotifyTokenService = spotifyTokenService;
        _authenticationService = authenticationService;
    }
    
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var appCredentialsService = new SpotifyAppCredentialsService();
        var credentials = await appCredentialsService.LoadCredentials();

        return Ok(credentials);
    }
    
    [Authorize]
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectToSpotify([FromBody] SpotifyConnectionDto dto)
    {
        var userId = _authenticationService.GetUserId();
        
        await _spotifyTokenService.CreateTokenSpotifyCredentials(dto, userId);

        return Ok();
    }
    
    [Authorize]
    [HttpGet("tokens")]
    public async Task<IActionResult> GetSpotifyTokens()
    {
        var userId = _authenticationService.GetUserId();

        var credentialsDto = await _spotifyTokenService.GetSpotifyTokenCredentials(userId);

        return Ok(credentialsDto);
    }
    
    [Authorize]
    [HttpGet("artists/top")]
    public async Task<IActionResult> GetSpotifyUsersTopArtists()
    {
        var userId = _authenticationService.GetUserId();

        var artists = await _spotifyContentRetriever.GetTopArtistsAsync(userId);

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("artists/followed")]
    public async Task<IActionResult> GetSpotifyUsersFollowedArtists()
    {
        var userId = _authenticationService.GetUserId();

        var artists = await _spotifyContentRetriever.GetFollowedArtistsAsync(userId);

        return Ok(artists);
    }
    
    [Authorize]
    [HttpPost("artists")]
    public async Task<IActionResult> DownloadSpotifyArtists()
    {
        var artists = await _spotifyContentManager.SaveRelevantArtists();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("genres/waged")]
    public async Task<IActionResult> GetWagedGenres()
    {
        var wagedGenres = await _spotifyContentManager.GetWagedGenres();

        return Ok(wagedGenres);
    }
}