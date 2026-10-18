namespace BandFounder.Application.Dtos.Spotify;

public sealed class LinkedSpotifyPlaylistResponse
{
    public required string Message { get; init; }

    public required string PlaylistName { get; init; }

    public required int NewArtistCount { get; init; }

    public required bool TruncatedAtTrackCap { get; init; }
}
