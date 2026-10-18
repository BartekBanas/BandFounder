using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities;

public class Artist : Entity
{
    [Key]
    public required string Id { get; set; } // Spotify ID for the artist

    public required string Name { get; set; }

    public int Popularity { get; set; }

    // Many-to-Many relationship with Genre
    public List<Genre> Genres { get; set; } = [];
    
    // Many-to-Many relationship with Account
    public virtual List<Account> Accounts { get; set; } = [];

    public virtual List<AccountArtist> AccountLinks { get; set; } = [];

    /// <summary>Takes the latest name and fills in popularity if it was never set.</summary>
    public void Enrich(string name, int popularity)
    {
        Name = name;

        if (Popularity == 0 && popularity > 0)
        {
            Popularity = popularity;
        }
    }
}
