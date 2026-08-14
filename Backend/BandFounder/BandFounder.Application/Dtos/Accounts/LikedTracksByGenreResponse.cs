namespace BandFounder.Application.Dtos.Accounts;

public class LikedTracksByGenreResponse
{
    public List<LikedTrackDto> Tracks { get; set; } = [];
    public int ScannedTrackCount { get; set; }
    public int RemainingTrackCount { get; set; }
    public int TotalAvailable { get; set; }
    public bool Truncated { get; set; }
}

public class LikedTracksByGenreProgressUpdate
{
    public required string Type { get; init; }
    public List<LikedTrackDto> Tracks { get; init; } = [];
    public int ScannedTrackCount { get; init; }
    public int RemainingTrackCount { get; init; }
    public int TotalAvailable { get; init; }
    public int FoundMatchCount { get; init; }
    public bool Truncated { get; init; }
}

public class LikedTrackDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? AlbumImageUrl { get; set; }
    public List<LikedTrackArtistDto> Artists { get; set; } = [];
}

public class LikedTrackArtistDto
{
    public string? Id { get; set; }
    public required string Name { get; set; }
    public List<string> Genres { get; set; } = [];
}
