using BandFounder.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/content")]
public class ContentController : Controller
{
    private readonly IContentService _contentService;

    public ContentController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet("artist")]
    public async Task<IActionResult> GetArtists()
    {
        var artists = await _contentService.GetArtistsAsync();
        
        return Ok(artists);
    }

    [HttpGet("genre")]
    public async Task<IActionResult> GetGenres()
    {
        var artists = await _contentService.GetGenresAsync();
        
        return Ok(artists);
    }

    [HttpGet("role")]
    public async Task<IActionResult> GetMusicianRoles()
    {
        var artists = await _contentService.GetMusicianRoles();
        
        return Ok(artists);
    }
}