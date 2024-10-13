using BandFounder.Application.Dtos.Spotify;
using BandFounder.Application.Services.Spotify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly ISpotifyContentManager _spotifyContentManager;
    private readonly ISpotifyCredentialsService _spotifyCredentialsService;

    public SpotifyBrokerController(
        ISpotifyContentRetriever spotifyContentRetriever,
        ISpotifyContentManager spotifyContentManager,
        ISpotifyCredentialsService spotifyCredentialsService)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _spotifyContentManager = spotifyContentManager;
        _spotifyCredentialsService = spotifyCredentialsService;
    }

    [Authorize]
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeSpotifyAccount([FromBody] SpotifyAuthorizationDto dto)
    {
        await _spotifyCredentialsService.CreateSpotifyCredentials(dto);

        return Ok();
    }

    [Authorize]
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var credentialsDto = await _spotifyCredentialsService.GetSpotifyCredentials();

        return Ok(credentialsDto);
    }

    [Authorize]
    [HttpGet("artists/top")]
    public async Task<IActionResult> GetSpotifyUsersTopArtists()
    {
        var artists = await _spotifyContentRetriever.GetTopArtistsAsync();

        return Ok(artists);
    }

    [Authorize]
    [HttpGet("artists/followed")]
    public async Task<IActionResult> GetSpotifyUsersFollowedArtists()
    {
        var artists = await _spotifyContentRetriever.GetFollowedArtistsAsync();

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
    [HttpPost("genres/waged")]
    public async Task<IActionResult> GetWagedGenres()
    {
        var wagedGenres = await _spotifyContentManager.GetWagedGenres();

        return Ok(wagedGenres);
    }
}