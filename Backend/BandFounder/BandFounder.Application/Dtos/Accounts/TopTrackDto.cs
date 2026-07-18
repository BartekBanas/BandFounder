namespace BandFounder.Application.Dtos.Accounts;

public class TopTrackDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ArtistNames { get; set; } = [];
}