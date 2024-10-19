using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities;

public class MusicProjectListing : Entity
{
    [Key]
    public Guid Id { get; set; }

    public required Guid AccountId { get; set; }
    public required string Name { get; set; }
    public virtual Account Owner { get; set; } = null!;

    public string? GenreName { get; set; }
    public virtual Genre? Genre { get; set; }

    public required MusicProjectType Type { get; set; }

    public virtual List<MusicianSlot> MusicianSlots { get; set; } = [];

    [MaxLength(100)]
    public string? Description { get; set; }
}

public enum MusicProjectType
{
    Band,
    CollaborativeSong
}