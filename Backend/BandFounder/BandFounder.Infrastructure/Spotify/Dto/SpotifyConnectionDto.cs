using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyConnectionDto
{
    [JsonPropertyName("code")]
    public required string AuthorizationCode { get; set; }
    
    [JsonPropertyName("base_app_url")]
    public required string BaseFrontendAppUrl { get; set; }
}