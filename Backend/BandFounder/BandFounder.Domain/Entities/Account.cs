using System.ComponentModel.DataAnnotations;
using BandFounder.Domain.Entities.Spotify;

namespace BandFounder.Domain.Entities;

public class Account : Entity
{ 
    [Key] 
    public required Guid Id { get; set; }
    
    [MinLength(3), MaxLength(32)]
    public required string Name { get; set; }

    public required string PasswordHash { get; set; }

    [EmailAddress]
    public required string Email { get; set; }
    
    public required DateTime DateCreated { get; set; } = DateTime.UtcNow;
    
    public virtual SpotifyCredentials? SpotifyCredentials { get; set; }
    
    // Many-to-Many relationship with Artist
    public virtual List<Artist> Artists { get; set; } = [];
}