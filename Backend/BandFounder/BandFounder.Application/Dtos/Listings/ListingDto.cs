using BandFounder.Application.Dtos.Accounts;

namespace BandFounder.Application.Dtos.Listings;

public class ListingDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public required AccountDto Owner { get; set; }
    public required string Name { get; set; }
    public string? Genre { get; set; }
    public required string Type { get; set; }
    public string? Description { get; set; }
    public DateTime DateCreated { get; set; }
    public required List<MusicianSlotDto> MusicianSlots { get; set; } = [];
}