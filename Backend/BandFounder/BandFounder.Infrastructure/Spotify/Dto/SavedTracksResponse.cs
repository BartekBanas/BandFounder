using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SavedTracksResponse
{
    [JsonPropertyName("items")] public required List<SavedTrackItem> Items { get; init; }

    [JsonPropertyName("next")] public string? Next { get; init; }

    [JsonPropertyName("total")] public int Total { get; init; }
}

public class SavedTrackItem
{
    [JsonPropertyName("track")] public SpotifyTrackDto? Track { get; init; }
}

public class SavedTracksFetchResult
{
    public required List<SpotifyTrackDto> Tracks { get; init; }
    public int TotalAvailable { get; init; }
    public bool Truncated { get; init; }
}

public class SavedTracksPage
{
    public required List<SpotifyTrackDto> Tracks { get; init; }
    public int TotalAvailable { get; init; }
    public bool HasMore { get; init; }
    public bool Truncated { get; init; }
}
