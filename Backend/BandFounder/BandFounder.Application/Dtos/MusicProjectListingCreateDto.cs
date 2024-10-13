using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos;

public class MusicProjectListingCreateDto
{
    public string? GenreName { get; set; }
    public required MusicProjectType Type { get; set; }
    public string? Description { get; set; }
    public List<MusicianSlotCreateDto> MusicianSlots { get; set; } = [];
}