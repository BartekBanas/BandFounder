using BandFounder.Application.Dtos;
using BandFounder.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/collaboration")]
public class MusicCollaborationController : Controller
{
    private readonly IMusicCollaborationService _musicCollaborationService;

    public MusicCollaborationController(IMusicCollaborationService musicCollaborationService)
    {
        _musicCollaborationService = musicCollaborationService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyMusicProjectListings()
    {
        var myProjects = await _musicCollaborationService.GetMyMusicProjectsAsync();
        
        return Ok(myProjects);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateMusicProjectListing(MusicProjectListingCreateDto dto)
    {
        await _musicCollaborationService.CreateMusicProjectListingAsync(dto);
        
        return Ok();
    }
}