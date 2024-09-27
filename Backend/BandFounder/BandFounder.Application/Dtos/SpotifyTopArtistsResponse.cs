using System.Text.Json.Serialization;

namespace BandFounder.Application.Dtos;

public class SpotifyTopArtistsResponse
{
    [JsonPropertyName("items")] public required List<Artist> Items { get; set; }
}