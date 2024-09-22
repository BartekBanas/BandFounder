using Microsoft.AspNetCore.Mvc;

namespace Bandfounder.Api;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var credentialManager = new SpotifyAppCredentialManager();
        await credentialManager.LoadCredentials();

        return Ok(credentialManager.SpotifyAppCredentials);
    }
}