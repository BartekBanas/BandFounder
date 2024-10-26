namespace BandFounder.Application.Dtos.Listings;

public class MusicianSlotDto
{
    public required Guid Id { get; set; }
    public required string Role { get; set; }
    public required string Status { get; set; }
}