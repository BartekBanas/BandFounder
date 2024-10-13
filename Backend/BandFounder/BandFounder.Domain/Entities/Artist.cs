using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities;

public class Artist : Entity
{
    [Key]
    public required string Id { get; set; } // Spotify ID for the artist

    public required string Name { get; set; }

    public int Popularity { get; set; }

    // Many-to-Many relationship with Genre
    public required List<Genre> Genres { get; set; } = [];
    
    // Many-to-Many relationship with Account
    public virtual List<Account> Accounts { get; set; } = [];
}