using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyPagingResponse<T>
{
    [JsonPropertyName("items")] public List<T> Items { get; init; } = [];

    [JsonPropertyName("next")] public string? Next { get; init; }

    [JsonPropertyName("total")] public int Total { get; init; }
}

public class SpotifyUserDto
{
    [JsonPropertyName("id")] public string? Id { get; init; }

    [JsonPropertyName("display_name")] public string? DisplayName { get; init; }
}

public class SpotifyExternalUrlsDto
{
    [JsonPropertyName("spotify")] public string? Spotify { get; init; }
}

public class SpotifyPlaylistDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }

    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;

    [JsonPropertyName("collaborative")] public bool Collaborative { get; init; }

    [JsonPropertyName("images")] public List<SpotifyImageDto>? Images { get; init; }

    [JsonPropertyName("owner")] public SpotifyUserDto? Owner { get; init; }

    [JsonPropertyName("external_urls")] public SpotifyExternalUrlsDto? ExternalUrls { get; init; }

    [JsonPropertyName("items")] public SpotifyPlaylistItemCountDto? Items { get; init; }

    [JsonPropertyName("tracks")] public SpotifyPlaylistItemCountDto? Tracks { get; init; }

    [JsonIgnore] public int ItemCount => Items?.Total ?? Tracks?.Total ?? 0;
}

public class SpotifyPlaylistItemCountDto
{
    [JsonPropertyName("total")] public int Total { get; init; }
}

public class SpotifyPlaylistItemDto
{
    [JsonPropertyName("added_by")] public SpotifyUserDto? AddedBy { get; init; }

    [JsonPropertyName("is_local")] public bool IsLocal { get; init; }

    [JsonPropertyName("item")] public SpotifyTrackDto? Item { get; init; }

    // Deprecated by Spotify in favour of "item"; still read so either response shape works.
    [JsonPropertyName("track")] public SpotifyTrackDto? Track { get; init; }

    [JsonIgnore] public SpotifyTrackDto? Content => Item ?? Track;
}

public class PlaylistTrackEntry
{
    public required SpotifyTrackDto Track { get; init; }
    public string? AddedById { get; init; }
}

public class PlaylistTracksPage
{
    public required List<PlaylistTrackEntry> Entries { get; init; }
    public int ScannedItemCount { get; init; }
    public int TotalAvailable { get; init; }
    public bool HasMore { get; init; }
}
