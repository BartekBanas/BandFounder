using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class SpotifyCredentials : Entity
{
    [Key, ForeignKey("Account")]
    public required Guid AccountId { get; init; }
    
    public required string AccessToken { get; set; }
    
    public required string RefreshToken { get; init; }
    
    public required DateTime ExpirationDate { get; set; }
    
    public virtual required Account Account { get; init; } = null!;
}