using BandFounder.Application.Dtos.Listings;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Application.Dtos.Backup;

public class AccountBackup
{
    public string Name { get; init; }
    public string Email { get; init; }
    public bool EmailOnNewMessage { get; init; } = true;
    public int EmailUnreadDelayMinutes { get; init; } = 1440;
    public SpotifyTokensDto? SpotifyTokens { get; set; }
    public ProfilePictureBackup? ProfilePicture { get; set; }
    public List<string> MusicianRoles { get; set; }
    public List<string> Artists { get; set; }
    public List<ListingCreateDto>? Listings { get; set; }
}