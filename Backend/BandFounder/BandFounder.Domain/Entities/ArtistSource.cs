namespace BandFounder.Domain.Entities;

public enum ArtistSourceKind
{
    Manual,
    SpotifySignIn,
    LinkedPlaylist
}

public readonly record struct ArtistSource
{
    public ArtistSourceKind Kind { get; }
    public string? LinkedPlaylistId { get; }

    private ArtistSource(ArtistSourceKind kind, string? linkedPlaylistId)
    {
        Kind = kind;
        LinkedPlaylistId = linkedPlaylistId;
    }

    public static ArtistSource Manual() => new(ArtistSourceKind.Manual, null);

    public static ArtistSource SpotifySignIn() => new(ArtistSourceKind.SpotifySignIn, null);

    public static ArtistSource FromLinkedPlaylist(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentException("Linked playlist id is required.", nameof(playlistId));
        }

        return new ArtistSource(ArtistSourceKind.LinkedPlaylist, playlistId);
    }

    public static ArtistSource From(ArtistSourceKind kind, string? linkedPlaylistId) =>
        kind switch
        {
            ArtistSourceKind.Manual => Manual(),
            ArtistSourceKind.SpotifySignIn => SpotifySignIn(),
            ArtistSourceKind.LinkedPlaylist => FromLinkedPlaylist(linkedPlaylistId ?? string.Empty),
        };
}
