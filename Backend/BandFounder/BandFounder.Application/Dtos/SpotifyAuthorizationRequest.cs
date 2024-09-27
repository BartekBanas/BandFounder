namespace BandFounder.Application.Dtos;

public class SpotifyAuthorizationRequest
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required int Duration { get; set; }
}