namespace BandFounder.Application.Dtos.Metadata;

public class ArtistDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> Genres { get; set; }
}