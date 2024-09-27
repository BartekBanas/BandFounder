using BandFounder.Application.Dtos.Spotify;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var credentialManager = new SpotifyAppCredentialsManager();
        var credentials = await credentialManager.LoadCredentials();

        return Ok(credentials);
    }

    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizeSpotifyAccount([FromBody] AuthorizationRequest request)
    {
        var spotifyAccessTokenService = new SpotifyAccessTokenService();
        await spotifyAccessTokenService.SaveAccessTokenAsync(request.AccessToken, request.RefreshToken, request.Duration);

        return Ok();
    }

    [HttpGet("top/artists")]
    public async Task<IActionResult> GetSpotifyUsersTopArtists()
    {
        var artists = await SpotifyContentService.GetTopArtistsAsync();

        return Ok(artists);
    }

    [HttpGet("followed/artists")]
    public async Task<IActionResult> GetSpotifyUsersFollowedArtists()
    {
        var artists = await SpotifyContentService.GetFollowedArtistsAsync();

        return Ok(artists);
    }
}