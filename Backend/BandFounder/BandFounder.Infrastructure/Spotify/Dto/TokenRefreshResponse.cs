using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class TokenRefreshResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    [JsonPropertyName("expires_in")] public required int Duration { get; init; }
}