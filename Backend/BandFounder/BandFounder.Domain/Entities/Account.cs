using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

[Table("Account")]
public class Account : Entity
{
    [Key] 
    public Guid Id { get; set; }
    
    [MinLength(3), MaxLength(32)]
    public required string Name { get; set; }

    public required string PasswordHash { get; set; }

    /// <summary>
    /// Incremented whenever the password changes. Embedded in JWTs so old tokens can be rejected.
    /// </summary>
    public int PasswordVersion { get; set; }

    [EmailAddress]
    public required string Email { get; set; }
    
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public virtual AccountNotificationPreferences NotificationPreferences { get; set; } = new();
    
    public virtual SpotifyTokens? SpotifyTokens { get; set; }
    
    // Many-to-Many relationship with Artist
    public virtual List<Artist> Artists { get; set; } = [];

    public virtual List<AccountArtist> ArtistLinks { get; set; } = [];

    public IEnumerable<Artist> HeldArtists() => ArtistLinks.Select(link => link.Artist);

    // One-to-Many relationship with LinkedPlaylist
    public virtual List<LinkedPlaylist> LinkedPlaylists { get; set; } = [];
    
    // One-to-Many relationship with Listing
    public virtual List<Listing> Listings { get; set; } = [];
    
    // One-to-Many relationship with Listing
    public virtual List<MusicianSlot> AssignedMusicianSlots  { get; set; } = [];
    
    // Many-to-Many relationship with MusicRole
    public virtual List<MusicianRole> MusicianRoles { get; set; } = [];
    
    // Many-to-Many relationship with Chatroom
    public virtual List<Chatroom> Chatrooms { get; set; } = [];
    
    public virtual ProfilePicture? ProfilePicture { get; set; }

    public bool HoldArtist(Artist artist, ArtistSource source)
    {
        var link = ArtistLinks.FirstOrDefault(existing => existing.ArtistId == artist.Id);
        if (link is not null)
        {
            link.AddSource(source);
            return false;
        }

        link = new AccountArtist
        {
            AccountId = Id,
            ArtistId = artist.Id,
            Artist = artist,
            Account = this
        };
        ArtistLinks.Add(link);

        if (!Artists.Any(held => held.Id == artist.Id))
        {
            Artists.Add(artist);
        }

        link.AddSource(source);
        return true;
    }

    public bool UnlinkPlaylist(string spotifyId)
    {
        var playlist = LinkedPlaylists.FirstOrDefault(linked => linked.SpotifyId == spotifyId);
        if (playlist is null)
        {
            return false;
        }

        LinkedPlaylists.Remove(playlist);
        DropArtistSource(ArtistSource.FromLinkedPlaylist(spotifyId));
        return true;
    }

    public void ClearMusicProfile()
    {
        ArtistLinks.Clear();
        Artists.Clear();
        LinkedPlaylists.Clear();
    }

    private void DropArtistSource(ArtistSource source)
    {
        foreach (var link in ArtistLinks.ToList())
        {
            if (!link.DropSource(source))
            {
                DropArtistLink(link);
            }
        }
    }

    private void DropArtistLink(AccountArtist link)
    {
        ArtistLinks.Remove(link);
        var artist = Artists.FirstOrDefault(held => held.Id == link.ArtistId);
        if (artist is not null)
        {
            Artists.Remove(artist);
        }
    }
}