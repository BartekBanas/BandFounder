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
    private readonly IMusicTasteComparisonService _musicTasteComparisonService;
    private readonly IAuthenticationService _authenticationService;

    public CollaborationController(
        ICollaborationService collaborationService,
        IMusicTasteComparisonService musicTasteComparisonService,
        IAuthenticationService authenticationService)
    {
        _collaborationService = collaborationService;
        _musicTasteComparisonService = musicTasteComparisonService;
        _authenticationService = authenticationService;
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
    public async Task<IActionResult> GetMusicProjectListings([FromQuery] FeedFilterOptions filterOptions)
    {
        var musicProjects = await _collaborationService.GetListingsFeedAsync(filterOptions);
        
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
    [HttpGet("{userId:guid}/commonTaste")]
    public async Task<IActionResult> GetCommonArtistsAndGenres(Guid userId)
    {
        var senderId = _authenticationService.GetUserId();
        var commonGenres = await _musicTasteComparisonService.GetCommonArtists(senderId, userId);
        var commonArtists = await _musicTasteComparisonService.GetCommonArtists(senderId, userId);
        
        var responseDto = new CommonGenresAndArtistsDto(commonArtists, commonGenres);
        
        return Ok(responseDto);
    }

    [Authorize]
    [HttpPost("contact/{listingId:guid}")]
    public async Task<IActionResult> CreateMusicProjectListing(Guid listingId)
    {
        await _collaborationService.Contact(listingId);
        
        return Ok();
    }

    [Authorize]
    [HttpPut("slot/{musicSlotId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid musicSlotId, SlotStatus status)
    {
        await _collaborationService.UpdateSlotStatus(musicSlotId, status);
        
        return Ok();
    }

    [Authorize]
    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid listingId)
    {
        await _collaborationService.DeleteListing(listingId);
        
        return Ok();
    }
}