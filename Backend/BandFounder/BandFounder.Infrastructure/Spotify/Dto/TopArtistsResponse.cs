using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class TopArtistsResponse
{
    [JsonPropertyName("items")] public required List<SpotifyArtistDto> Items { get; init; }
}