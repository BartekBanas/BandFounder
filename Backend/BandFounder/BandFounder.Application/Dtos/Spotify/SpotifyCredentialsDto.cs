using System.ComponentModel.DataAnnotations;

namespace BandFounder.Application.Dtos.Spotify;

public class SpotifyCredentialsDto
{
    [Required] public required string AccessToken { get; set; }
    [Required] public required string RefreshToken { get; set; }
    [Required] public required DateTime ExpirationDate { get; set; }
}