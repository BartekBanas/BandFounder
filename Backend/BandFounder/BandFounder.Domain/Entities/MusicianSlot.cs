using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class MusicianSlot : Entity
{ 
    [Key]
    public Guid Id { get; set; }

    public virtual MusicianRole Role { get; set; } = null!;

    [Column(TypeName = "text")]
    public virtual SlotStatus Status { get; set; }

    public virtual Guid ListingId { get; set; }
    public virtual Listing Listing { get; set; } = null!;
}

public enum SlotStatus
{
    Available,
    Filled
}