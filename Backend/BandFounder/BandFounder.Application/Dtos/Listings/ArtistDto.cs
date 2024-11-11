namespace BandFounder.Application.Dtos.Listings;

public class ArtistDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> Genres { get; set; }
}