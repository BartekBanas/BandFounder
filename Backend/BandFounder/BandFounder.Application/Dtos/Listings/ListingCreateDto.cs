using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Listings;

public class ListingCreateDto
{
    public required string Name { get; set; }
    public string? GenreName { get; set; }
    public required ListingType Type { get; set; }
    public string? Description { get; set; }
    public required List<MusicianSlotCreateDto> MusicianSlots { get; set; } = [];
}