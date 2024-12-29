using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api")]
public class SpotifyBrokerController : ControllerBase
{
    private readonly ISpotifyConnectionService _spotifyConnectionService;
    private readonly IAuthenticationService _authenticationService;

    public SpotifyBrokerController(
        ISpotifyConnectionService spotifyConnectionService,
        IAuthenticationService authenticationService)
    {
        _spotifyConnectionService = spotifyConnectionService;
        _authenticationService = authenticationService;
    }

    [HttpGet("spotify/app/clientId")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var appCredentialsService = new SpotifyAppCredentialsService();
        var credentials = await appCredentialsService.LoadCredentials();

        return Ok(credentials.ClientId);
    }
    
    [Authorize]
    [HttpPost("spotify/tokens")]
    public async Task<IActionResult> ConnectToSpotify([FromBody] SpotifyConnectionDto dto)
    {
        var userId = _authenticationService.GetUserId();
        
        await _spotifyConnectionService.LinkAccountToSpotify(dto, userId);

        return Ok();
    }
    
    [Authorize]
    [Obsolete("Use /spotify/tokens instead. This endpoint is for development purposes only.")]
    [HttpPost("spotify/tokens/manual")]
    public async Task<IActionResult> AddSpotifyTokens([FromBody] SpotifyTokensDto dto)
    {
        await _spotifyConnectionService.CreateSpotifyTokens(dto, _authenticationService.GetUserId());

        return Ok();
    }
    
    [Authorize]
    [HttpGet("spotify/tokens")]
    public async Task<IActionResult> GetSpotifyTokens()
    {
        var userId = _authenticationService.GetUserId();

        var credentialsDto = await _spotifyConnectionService.GetSpotifyTokens(userId);

        return Ok(credentialsDto);
    }
    
    [Authorize]
    [HttpPost("spotify/update-artists")]
    public async Task<IActionResult> UpdateArtistsFromSpotify()
    {
        var userId = _authenticationService.GetUserId();
        
        var newlyAddedArtists = await _spotifyConnectionService.SaveRelevantArtists(userId);

        return Ok(newlyAddedArtists);
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists/spotify/top")]
    public async Task<IActionResult> GetUsersSpotifyTopArtists([FromRoute] Guid accountId)
    {
        var artistDtoList = await _spotifyConnectionService.GetTopArtistsAsync(accountId, 10);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists/spotify/followed")]
    public async Task<IActionResult> GetUsersSpotifyFollowedArtists([FromRoute] Guid accountId)
    {
        var artistDtoList = await _spotifyConnectionService.GetFollowedArtistsAsync(accountId);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
}