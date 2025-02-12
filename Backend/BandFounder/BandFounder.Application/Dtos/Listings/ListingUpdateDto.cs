using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Listings;

public class ListingUpdateDto
{
    public required string Name { get; set; }
    public string? Genre { get; set; }
    public required ListingType Type { get; set; }
    public string? Description { get; set; }
    public required List<MusicianSlotUpdateDto> MusicianSlots { get; set; } = [];
}