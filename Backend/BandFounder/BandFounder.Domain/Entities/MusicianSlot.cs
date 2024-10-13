using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities;

public class MusicianSlot : Entity
{ 
    [Key]
    public Guid Id { get; set; }

    public virtual MusicianRole Role { get; set; } = null!;

    public virtual SlotStatus Status { get; set; }

    public virtual MusicProjectListing Listing { get; set; } = null!;
}

public enum SlotStatus
{
    Available,
    Filled
}