using System.Text.Json.Serialization;

namespace BandFounder.Application.Dtos.Spotify;

public class ArtistDto
{
    [JsonPropertyName("genres")] public required List<string> Genres { get; set; }

    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("popularity")] public int Popularity { get; set; }
}