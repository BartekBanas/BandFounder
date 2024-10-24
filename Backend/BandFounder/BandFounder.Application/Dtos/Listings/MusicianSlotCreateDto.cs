using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Listings;

public class MusicianSlotCreateDto
{
    public required string RoleName { get; set; }
    public SlotStatus Status { get; set; } = SlotStatus.Available;

}