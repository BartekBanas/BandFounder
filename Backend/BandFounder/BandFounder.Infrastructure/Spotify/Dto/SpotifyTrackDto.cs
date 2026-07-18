using System.Text.Json.Serialization;

namespace BandFounder.Infrastructure.Spotify.Dto;

public class SpotifyTrackDto
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("album")] public SpotifyAlbumDto? Album { get; set; }

    [JsonPropertyName("artists")] public List<SpotifySimplifiedArtistDto> Artists { get; set; } = [];
}

public class SpotifyAlbumDto
{
    [JsonPropertyName("images")] public List<SpotifyImageDto> Images { get; set; } = [];
}

public class SpotifySimplifiedArtistDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("name")] public required string Name { get; set; }
}