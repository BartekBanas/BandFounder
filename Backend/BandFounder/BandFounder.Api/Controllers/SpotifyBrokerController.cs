using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api")]
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
        await _spotifyConnectionService.LinkAccountToSpotify(dto);

        return Ok();
    }
    
    [Authorize]
    [HttpGet("spotify/tokens")]
    public async Task<IActionResult> GetSpotifyTokens()
    {
        var userId = _authenticationService.GetUserId();

        var credentialsDto = await _spotifyTokenService.GetSpotifyTokens(userId);

        return Ok(credentialsDto);
    }
    
    [Authorize]
    [HttpPost("spotify/update-artists")]
    public async Task<IActionResult> UpdateArtistsFromSpotify()
    {
        var newlyAddedArtists = await _spotifyConnectionService.SaveRelevantArtists();

        return Ok(newlyAddedArtists);
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists/spotify/top")]
    public async Task<IActionResult> GetUsersSpotifyTopArtists([FromRoute] Guid accountId)
    {
        var artistDtoList = await _spotifyContentRetriever.GetTopArtistsAsync(accountId, 10);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
    
    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists/spotify/followed")]
    public async Task<IActionResult> GetUsersSpotifyFollowedArtists([FromRoute] Guid accountId)
    {
        var artistDtoList = await _spotifyContentRetriever.GetFollowedArtistsAsync(accountId);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }
}