using System.ComponentModel.DataAnnotations;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyCredentialsDto
{
    [Required] public required string AccessToken { get; set; }
    [Required] public required string RefreshToken { get; set; }
    [Required] public required DateTime ExpirationDate { get; set; }
}