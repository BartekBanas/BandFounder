namespace BandFounder.Application.Dtos.Listings;

public class CommonGenresAndArtistsDto
{
    public IEnumerable<string> CommonArtists { get; set; }
    public IEnumerable<string> CommonGenres { get; set; }
    
    public CommonGenresAndArtistsDto(IEnumerable<string> commonArtists, IEnumerable<string> commonGenres)
    {
        CommonArtists = commonArtists;
        CommonGenres = commonGenres;
    }
}