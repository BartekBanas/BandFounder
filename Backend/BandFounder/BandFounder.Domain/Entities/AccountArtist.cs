using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class AccountArtist : Entity
{
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public required string ArtistId { get; set; }
    public virtual Artist Artist { get; set; } = null!;

    public virtual List<AccountArtistSource> Sources { get; set; } = [];

    public void AddSource(ArtistSource source)
    {
        if (Sources.Any(existing =>
                existing.Kind == source.Kind && existing.LinkedPlaylistId == source.LinkedPlaylistId))
        {
            return;
        }

        Sources.Add(new AccountArtistSource
        {
            AccountId = AccountId,
            ArtistId = ArtistId,
            Kind = source.Kind,
            LinkedPlaylistId = source.LinkedPlaylistId
        });
    }

    public bool DropSource(ArtistSource source)
    {
        var existing = Sources.FirstOrDefault(held =>
            held.Kind == source.Kind && held.LinkedPlaylistId == source.LinkedPlaylistId);
        if (existing is null)
        {
            return Sources.Count > 0;
        }

        Sources.Remove(existing);
        return Sources.Count > 0;
    }
}

public class AccountArtistSource : Entity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }
    public required string ArtistId { get; set; }
    public virtual AccountArtist AccountArtist { get; set; } = null!;

    [Column(TypeName = "text")]
    public ArtistSourceKind Kind { get; set; }

    public string? LinkedPlaylistId { get; set; }

    public ArtistSource ToSource() => ArtistSource.From(Kind, LinkedPlaylistId);
}
