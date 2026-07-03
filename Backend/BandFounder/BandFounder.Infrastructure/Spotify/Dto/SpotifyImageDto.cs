using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyImageDto
{
    [JsonPropertyName("url")] public required string Url { get; set; }

    [JsonPropertyName("height")] public int? Height { get; set; }

    [JsonPropertyName("width")] public int? Width { get; set; }
}
