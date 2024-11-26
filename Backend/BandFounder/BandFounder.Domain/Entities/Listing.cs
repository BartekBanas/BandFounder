using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace BandFounder.Domain.Entities;

public class Listing : Entity
{
    [Key]
    public Guid Id { get; set; }

    public required string Name { get; set; }
    
    public required Guid OwnerId { get; set; }
    public virtual Account Owner { get; set; } = null!;

    public string? GenreName { get; set; }
    public virtual Genre? Genre { get; set; }

    [Column(TypeName = "text")]
    public required ListingType Type { get; set; }

    public virtual List<MusicianSlot> MusicianSlots { get; set; } = [];

    public string? Description { get; set; }
    
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}

public enum ListingType
{
    [EnumMember(Value = "Active")] Band,
    [EnumMember(Value = "Active")] CollaborativeSong
}