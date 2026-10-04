namespace BandFounder.Application.Dtos.Accounts;

public class SpotifyPlaylistSummaryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public string? OwnerName { get; init; }
    public bool OwnedByMe { get; init; }
    public bool Collaborative { get; init; }
    public int TrackCount { get; init; }
    public required string Url { get; init; }
}

public class PlaylistContributorDto
{
    /// <summary>Spotify user id, or an empty string for items Spotify returns without an adder.</summary>
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public bool IsCurrentUser { get; init; }
    public int TrackCount { get; init; }
}

public class PlaylistContributorsPreviewDto
{
    public required SpotifyPlaylistSummaryDto Playlist { get; init; }
    public List<PlaylistContributorDto> Contributors { get; init; } = [];
    public int TrackCount { get; init; }
    public int SkippedItemCount { get; init; }
}

public class PlaylistCopyRequestDto
{
    public string? SourcePlaylistId { get; init; }
    public List<string>? ContributorIds { get; init; }
    public string? TargetPlaylistId { get; init; }
    public string? NewPlaylistName { get; init; }
}

public static class PlaylistCopyPhase
{
    public const string ReadingSource = "reading-source";
    public const string ReadingTarget = "reading-target";
    public const string Adding = "adding";
}

public class PlaylistCopyProgressUpdate
{
    public required string Type { get; init; }
    public string? Phase { get; init; }
    public int Processed { get; init; }
    public int Total { get; init; }
    public int SelectedTrackCount { get; init; }
    public int AddedCount { get; init; }
    public int SkippedDuplicateCount { get; init; }
    public SpotifyPlaylistSummaryDto? TargetPlaylist { get; init; }
    public bool CreatedTarget { get; init; }
    public string? FailureMessage { get; init; }
}
