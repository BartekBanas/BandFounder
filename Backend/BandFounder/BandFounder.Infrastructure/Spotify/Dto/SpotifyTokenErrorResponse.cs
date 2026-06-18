using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyTokenErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
