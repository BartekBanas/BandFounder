using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Application.Dtos.Accounts;

public class AccountDetailedDto
{
    public string Name { get; init; }
    public string Email { get; init; }
    public SpotifyTokensDto? SpotifyTokens { get; set; }
    public List<string> MusicianRoles { get; set; }
    public List<string> Artists { get; set; }
}