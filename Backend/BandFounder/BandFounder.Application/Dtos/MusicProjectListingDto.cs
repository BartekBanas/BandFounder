using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos;

public class MusicProjectListingDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? GenreName { get; set; }
    public required MusicProjectType Type { get; set; }
    public string? Description { get; set; }
    public required List<MusicianSlotDto> MusicianSlots { get; set; } = [];
}