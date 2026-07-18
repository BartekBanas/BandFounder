using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class TopTracksResponse
{
    [JsonPropertyName("items")] public required List<SpotifyTrackDto> Items { get; init; }
}