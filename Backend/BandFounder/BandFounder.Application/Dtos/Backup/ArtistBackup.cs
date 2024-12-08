namespace BandFounder.Application.Dtos.Backup;

public class ArtistBackup
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Popularity { get; set; }
    public List<string> Genres { get; set; }
}