using System.Text.Json.Serialization;

namespace BandFounder.Application.Dtos.Spotify;

public class FollowedArtistsResponse
{
    [JsonPropertyName("artists")]
    public required ArtistsResponse Artists { get; init; }
}

public class ArtistsResponse
{
    [JsonPropertyName("items")]
    public required List<ArtistDto> Items { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }
}