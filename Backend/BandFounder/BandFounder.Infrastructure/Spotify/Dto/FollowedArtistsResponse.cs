using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class FollowedArtistsResponse
{
    [JsonPropertyName("artists")]
    public required ArtistsResponse Artists { get; init; }
}

public class ArtistsResponse
{
    [JsonPropertyName("items")]
    public required List<SpotifyArtistDto> Items { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }
}