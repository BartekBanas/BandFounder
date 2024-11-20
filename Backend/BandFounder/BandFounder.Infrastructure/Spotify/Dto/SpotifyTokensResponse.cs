using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyTokensResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}