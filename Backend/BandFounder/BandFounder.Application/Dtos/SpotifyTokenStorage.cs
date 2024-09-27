namespace BandFounder.Application.Dtos;

public class SpotifyTokenStorage
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime ExpirationDate { get; set; }
}