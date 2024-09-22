using Microsoft.AspNetCore.Mvc;

namespace Bandfounder.Api;

[Route("api/spotifyBroker")]
public class SpotifyBrokerController : ControllerBase
{
    [HttpGet("credentials")]
    public async Task<IActionResult> GetSpotifyAppConfig()
    {
        var configManager = new SpotifyAppConfigManager();
        await configManager.LoadConfigAsync();

        return Ok(configManager.Config);
    }
}