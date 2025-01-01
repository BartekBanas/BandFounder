using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Exceptions;
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
            throw new NotFoundException("Listing not found");
        }
        
        return Ok(listing.ToDto());
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMusicProjectListings([FromQuery] FeedFilterOptions filterOptions)
    {
        var listingsFeedDto = await _listingService.GetListingsFeedAsync(filterOptions);
        
        return Ok(listingsFeedDto);
    }

    [Obsolete, Authorize, HttpGet("me")]
    public async Task<IActionResult> GetMyMusicProjectListings()
    {
        var myListings = await _listingService.GetUserListingsAsync();
        var dto = myListings.ToDto();
        
        return Ok(dto);
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
        var createdChatroomDto = await _listingService.ContactOwner(listingId);
        
        return Ok(createdChatroomDto);
    }

    [Authorize]
    [HttpPut("slots/{musicSlotId:guid}")]
    public async Task<IActionResult> UpdateMusicianSlotStatus([FromRoute] Guid musicSlotId, SlotStatus status)
    {
        await _listingService.UpdateSlotStatus(musicSlotId, status);
        
        return Ok();
    }

    [Authorize]
    [HttpPut("{listingId:guid}")]
    public async Task<IActionResult> UpdateListing([FromRoute] Guid listingId, [FromBody] ListingCreateDto dto)
    {
        await _listingService.UpdateListing(listingId, dto);
        
        return Ok();
    }

    [Authorize]
    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> DeleteListing([FromRoute] Guid listingId)
    {
        await _listingService.DeleteListing(listingId);
        
        return Ok();
    }
}