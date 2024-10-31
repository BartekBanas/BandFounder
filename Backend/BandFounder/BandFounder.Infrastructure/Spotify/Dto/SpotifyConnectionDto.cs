namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyConnectionDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required int Duration { get; set; }
}