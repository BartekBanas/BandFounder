using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SeveralArtistsResponse
{
    [JsonPropertyName("artists")] public required List<SpotifyArtistDto?> Artists { get; init; }
}
