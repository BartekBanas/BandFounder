namespace BandFounder.Application.Dtos.Backup;

public class LinkedPlaylistBackup
{
    public required string SpotifyId { get; init; }
    public required string Name { get; init; }
    public DateTime LinkedAt { get; init; }
}
