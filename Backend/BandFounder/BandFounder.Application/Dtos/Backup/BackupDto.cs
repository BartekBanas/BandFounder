namespace BandFounder.Application.Dtos.Backup;

public class BackupDto
{
    public IEnumerable<AccountBackup> Accounts { get; set; }
    public IEnumerable<ArtistBackup> Artists { get; set; }
}