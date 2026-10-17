using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Backup;

public class AccountArtistBackup
{
    public required string Name { get; init; }
    public List<ArtistSourceBackup> Sources { get; init; } = [];
}

public class ArtistSourceBackup
{
    public ArtistSourceKind Kind { get; init; }
    public string? LinkedPlaylistId { get; init; }

    public ArtistSource ToSource() => ArtistSource.From(Kind, LinkedPlaylistId);
}
