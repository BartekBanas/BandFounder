namespace BandFounder.Domain.Entities;

/// <summary>Incoming artist details (from Spotify or a backup) to match against or add to the artist catalog.</summary>
public sealed record ArtistData(string? Id, string Name)
{
    private const string SpotifyUriPrefix = "spotify:artist:";

    public IReadOnlyList<string> Genres { get; init; } = [];

    public int Popularity { get; init; }

    /// <summary>The bare Spotify artist id. Accepts a raw id or a <c>spotify:artist:</c> URI. Null when no id was given.</summary>
    public string? SpotifyId => string.IsNullOrWhiteSpace(Id)
        ? null
        : Id.StartsWith(SpotifyUriPrefix, StringComparison.OrdinalIgnoreCase)
            ? Id[SpotifyUriPrefix.Length..]
            : Id;
}
