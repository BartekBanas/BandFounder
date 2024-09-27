using System.Text.Json.Serialization;

namespace BandFounder.Application.Dtos.Spotify;

public class TopArtistsResponse
{
    [JsonPropertyName("items")] public required List<Artist> Items { get; init; }
}