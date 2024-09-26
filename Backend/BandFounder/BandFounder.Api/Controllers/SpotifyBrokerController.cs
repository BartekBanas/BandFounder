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
}