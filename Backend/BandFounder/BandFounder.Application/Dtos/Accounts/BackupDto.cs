using BandFounder.Application.Dtos.Listings;

namespace BandFounder.Application.Dtos.Accounts;

public class BackupDto
{
    public IEnumerable<AccountDetailedDto> Accounts { get; set; }
    public IEnumerable<ArtistDto> Artists { get; set; }
}