using System.Text.Json.Serialization;

namespace BandFounder.Application.Dtos;

public class SpotifyTokenRefreshResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    [JsonPropertyName("expires_in")] public required int Duration { get; init; }
}