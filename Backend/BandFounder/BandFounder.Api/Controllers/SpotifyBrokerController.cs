using BandFounder.Application.SpotifyCredentialsService;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var credentialManager = new SpotifyCredentialsManager();
        await credentialManager.LoadCredentials();

        return Ok(credentialManager.SpotifyCredentials);
    }
}