namespace BandFounder.Application.Dtos.Spotify;

public class AuthorizationRequest
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required int Duration { get; set; }
}