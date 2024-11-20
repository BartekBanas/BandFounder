using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly ISpotifyConnectionService _spotifyConnectionService;
    private readonly ISpotifyTokenService _spotifyTokenService;
    
    private readonly IAuthenticationService _authenticationService;
    
    public SpotifyBrokerController(
        ISpotifyContentRetriever spotifyContentRetriever,
        ISpotifyConnectionService spotifyConnectionService,
        ISpotifyTokenService spotifyTokenService, 
        IAuthenticationService authenticationService)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _spotifyConnectionService = spotifyConnectionService;
        _spotifyTokenService = spotifyTokenService;
        _authenticationService = authenticationService;
    }
    
    [HttpGet("clientId")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var appCredentialsService = new SpotifyAppCredentialsService();
        var credentials = await appCredentialsService.LoadCredentials();

        return Ok(credentials.ClientId);
    }
    
    [Authorize]
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectToSpotify([FromBody] SpotifyConnectionDto dto)
    {
        await _spotifyConnectionService.LinkAccountToSpotify(dto);

        return Ok();
    }
    
    [Authorize]
    [HttpGet("tokens")]
    public async Task<IActionResult> GetSpotifyTokens()
    {
        var userId = _authenticationService.GetUserId();

        var credentialsDto = await _spotifyTokenService.GetSpotifyTokens(userId);

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
}