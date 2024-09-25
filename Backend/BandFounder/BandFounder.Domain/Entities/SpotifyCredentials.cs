using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class SpotifyCredentials
{
    [Key]
    public required Guid Id { get; set; }

    [ForeignKey("Account")]
    public required Guid AccountId { get; set; }

    public required string AccessToken { get; set; }
    
    public required string RefreshToken { get; set; }
    
    public required DateTime ExpirationDate { get; set; }
    
    public virtual required Account Account { get; set; } = null!;
}