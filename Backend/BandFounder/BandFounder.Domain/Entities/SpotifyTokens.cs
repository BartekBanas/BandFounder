using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class SpotifyTokens : Entity
{
    [Key, ForeignKey("Account")]
    public required Guid AccountId { get; init; }
    
    public required string AccessToken { get; set; }
    
    public required string RefreshToken { get; set; }
    
    public required DateTime ExpirationDate { get; set; }

    /// <summary>UTC time of the last successful taste-profile sync (top + followed artists).</summary>
    public DateTime? ArtistsSyncedAt { get; set; }
    
    public virtual Account Account { get; init; } = null!;
}