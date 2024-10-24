using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/collaboration")]
public class CollaborationController : Controller
{
    private readonly ICollaborationService _collaborationService;

    public CollaborationController(ICollaborationService collaborationService)
    {
        _collaborationService = collaborationService;
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMusicProjectListing([FromRoute] Guid id)
    {
        var listing = await _collaborationService.GetListingAsync(id);
        
        return Ok(listing);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMusicProjectListings()
    {
        var musicProjects = await _collaborationService.GetListingsFeedAsync();
        
        return Ok(musicProjects);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyMusicProjectListings()
    {
        var myProjects = await _collaborationService.GetMyMusicProjectsAsync();
        
        return Ok(myProjects);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateMusicProjectListing(MusicProjectListingCreateDto dto)
    {
        await _collaborationService.CreateMusicProjectListingAsync(dto);
        
        return Ok();
    }

    [Authorize]
    [HttpPut("slot/{musicSlotId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid musicSlotId, SlotStatus status)
    {
        await _collaborationService.UpdateSlotStatus(musicSlotId, status);
        
        return Ok();
    }
}