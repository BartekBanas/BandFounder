using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

[Table("LinkedPlaylist")]
public class LinkedPlaylist : Entity
{
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public required string SpotifyId { get; set; }

    public required string Name { get; set; }

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
