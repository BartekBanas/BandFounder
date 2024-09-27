namespace BandFounder.Application.Dtos.Spotify;

public class TokenStorage
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime ExpirationDate { get; set; }
}