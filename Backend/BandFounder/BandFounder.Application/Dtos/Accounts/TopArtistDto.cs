namespace BandFounder.Application.Dtos.Accounts;

public class TopArtistDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
}
