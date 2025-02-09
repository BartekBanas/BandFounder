using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Listings;

public class MusicianSlotUpdateDto
{
    public required Guid? Id { get; set; }
    public required string Role { get; set; }
    public required SlotStatus Status { get; set; }
}