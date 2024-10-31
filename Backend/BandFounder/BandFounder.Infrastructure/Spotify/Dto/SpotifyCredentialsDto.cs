namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyCredentialsDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime ExpirationDate { get; set; }
}