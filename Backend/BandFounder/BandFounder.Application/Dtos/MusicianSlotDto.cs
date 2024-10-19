using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos;

public class MusicianSlotDto
{
    public required Guid Id { get; set; }
    public required string Role { get; set; }
    public required SlotStatus Status { get; set; }
}