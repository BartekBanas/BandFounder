using BandFounder.Application.Dtos;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
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
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMusicProjectListing([FromRoute] Guid id)
    {
        var listing = await _musicCollaborationService.GetListingAsync(id);
        
        return Ok(listing);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMusicProjectListings()
    {
        var musicProjects = await _musicCollaborationService.GetMusicProjectsAsync();
        
        return Ok(musicProjects);
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

    [Authorize]
    [HttpPut("slot/{musicSlotId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid musicSlotId, SlotStatus status)
    {
        await _musicCollaborationService.UpdateSlotStatus(musicSlotId, status);
        
        return Ok();
    }
}