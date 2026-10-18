using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public sealed class SeveralArtistsResponse
{
    [JsonPropertyName("artists")]
    public required List<SpotifyArtistDto?> Artists { get; init; }
}
