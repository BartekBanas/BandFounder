using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Backup;

public class BackupDto
{
    public IEnumerable<AccountBackup> Accounts { get; set; }
    public IEnumerable<Artist> Artists { get; set; }
}