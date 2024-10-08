using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/ping")]
public class PingController : Controller
{
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok();
    }
}