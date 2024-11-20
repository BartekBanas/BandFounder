using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyConnectionDto
{
    [JsonPropertyName("code")]
    public string AuthorizationCode { get; set; }
    
    [JsonPropertyName("base_app_url")]
    public string BaseFrontendAppUrl { get; set; }
}