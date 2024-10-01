using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities.Spotify;

public class Genre : Entity
{
    [Key]
    public required string Name { get; set; } // Genre name as the primary key

    // Many-to-Many relationship with Artist
    public List<Artist> Artists { get; set; } = [];
}