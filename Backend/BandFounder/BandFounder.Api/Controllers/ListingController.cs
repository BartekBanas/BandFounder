using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Error;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingController : Controller
{
    private readonly IListingService _listingService;

    public ListingController(IListingService listingService)
    {
        _listingService = listingService;
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMusicProjectListing([FromRoute] Guid id)
    {
        var listing = await _listingService.GetListingAsync(id);

        if (listing is null)
        {
            throw new NotFoundError("Listing not found");
        }
        
        return Ok(listing.ToDto());
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMusicProjectListings([FromQuery] FeedFilterOptions filterOptions)
    {
        var musicProjects = await _listingService.GetListingsFeedAsync(filterOptions);
        
        return Ok(musicProjects);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyMusicProjectListings()
    {
        var myProjects = await _listingService.GetMyListingAsync();
        
        return Ok(myProjects);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateListing(ListingCreateDto dto)
    {
        await _listingService.CreateListingAsync(dto);
        
        return Ok();
    }

    [Authorize]
    [HttpGet("{listingId:guid}/commonTaste")]
    public async Task<IActionResult> GetCommonArtistsAndGenres(Guid listingId)
    {
        var responseDto = await _listingService.GetCommonArtistsAndGenresWithListingsAsync(listingId);
        
        return Ok(responseDto);
    }

    [Authorize]
    [HttpPost("{listingId:guid}/contact")]
    public async Task<IActionResult> ContactListingOwner(Guid listingId)
    {
        await _listingService.ContactOwner(listingId);
        
        return Ok();
    }

    [Authorize]
    [HttpPut("slots/{musicSlotId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid musicSlotId, SlotStatus status)
    {
        await _listingService.UpdateSlotStatus(musicSlotId, status);
        
        return Ok();
    }

    [Authorize]
    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid listingId)
    {
        await _listingService.DeleteListing(listingId);
        
        return Ok();
    }
}