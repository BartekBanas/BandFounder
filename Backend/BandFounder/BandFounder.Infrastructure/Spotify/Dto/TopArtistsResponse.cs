using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class TopArtistsResponse
{
    [JsonPropertyName("items")] public required List<ArtistDto> Items { get; init; }
}