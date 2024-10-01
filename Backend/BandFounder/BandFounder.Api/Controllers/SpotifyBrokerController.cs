using BandFounder.Application.Dtos.Spotify;
using BandFounder.Application.Services.Spotify;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    private readonly ISpotifyContentService _spotifyContentService;
    private readonly ISpotifyCredentialsService _spotifyCredentialsService;

    public SpotifyBrokerController(
        ISpotifyContentService spotifyContentService,
        ISpotifyCredentialsService spotifyCredentialsService)
    {
        _spotifyContentService = spotifyContentService;
        _spotifyCredentialsService = spotifyCredentialsService;
    }

    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeSpotifyAccount([FromBody] SpotifyAuthorizationDto dto)
    {
        await _spotifyCredentialsService.CreateSpotifyCredentials(dto);

        return Ok();
    }

    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var credentialsDto = await _spotifyCredentialsService.GetSpotifyCredentials();

        return Ok(credentialsDto);
    }

    [HttpGet("top/artists")]
    public async Task<IActionResult> GetSpotifyUsersTopArtists()
    {
        var artists = await _spotifyContentService.GetTopArtistsAsync();

        return Ok(artists);
    }

    [HttpGet("followed/artists")]
    public async Task<IActionResult> GetSpotifyUsersFollowedArtists()
    {
        var artists = await _spotifyContentService.GetFollowedArtistsAsync();

        return Ok(artists);
    }

    [HttpPost("artists")]
    public async Task<IActionResult> DownloadSpotifyArtists()
    {
        var artists = await _spotifyContentService.SaveRelevantArtists();

        return Ok(artists);
    }
}