namespace BandFounder.Infrastructure.Spotify.Dto;

public sealed class SpotifyPlaylistRead
{
    public required string SpotifyId { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<SpotifyPlaylistArtist> Artists { get; init; }

    public required int TrackCount { get; init; }

    public required bool TruncatedAtTrackCap { get; init; }
}

public sealed class SpotifyPlaylistArtist
{
    public required string Id { get; init; }

    public required string Name { get; init; }
}
