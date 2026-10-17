using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public sealed class SpotifyPlaylistMetadataDto
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("name")] public required string Name { get; set; }
}

public sealed class SpotifyPlaylistItemsPageDto
{
    [JsonPropertyName("items")] public List<SpotifyPlaylistItemRowDto> Items { get; set; } = [];

    [JsonPropertyName("next")] public string? Next { get; set; }

    [JsonPropertyName("total")] public int Total { get; set; }
}

public sealed class SpotifyPlaylistItemRowDto
{
    [JsonPropertyName("item")] public SpotifyPlaylistItemDto? Item { get; set; }

    [JsonPropertyName("is_local")] public bool IsLocal { get; set; }
}

public sealed class SpotifyPlaylistItemDto
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("artists")] public List<SpotifySimplifiedArtistDto> Artists { get; set; } = [];
}
