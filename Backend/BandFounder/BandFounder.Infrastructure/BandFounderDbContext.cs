using BandFounder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public class BandFounderDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<ProfilePicture> ProfilePictures { get; set; }
    public DbSet<Chatroom> Chatrooms { get; set; }
    public DbSet<Account> Messages { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Genre> Genres { get; set; }
    
    public DbSet<Listing> Listings { get; set; }
    public DbSet<MusicianSlot> MusicianSlots { get; set; }
    public DbSet<MusicianRole> MusicianRoles { get; set; }
    
    public DbSet<SpotifyTokens> SpotifyTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuring relationships
        modelBuilder.Entity<Account>()
            .HasOne(account => account.SpotifyTokens)
            .WithOne(spotifyCredentials => spotifyCredentials.Account)
            .HasForeignKey<SpotifyTokens>(spotifyCredentials => spotifyCredentials.AccountId);
        
        // Configuring many-to-many relationship between Account and Artist
        modelBuilder.Entity<Account>()
            .HasMany(account => account.Artists)
            .WithMany(artist => artist.Accounts);
        
        modelBuilder.Entity<Artist>()
            .HasMany(artist => artist.Genres)
            .WithMany(genre => genre.Artists);
        
        // Many-to-One relationship: Listing has one owner (Account)
        modelBuilder.Entity<Listing>()
            .HasOne(projectListing => projectListing.Owner)
            .WithMany(account => account.Listings)
            .HasForeignKey(projectListing => projectListing.OwnerId);

        // Many-to-One relationship: Listing has an optional Genre
        modelBuilder.Entity<Listing>()
            .HasOne(projectListing => projectListing.Genre)
            .WithMany()
            .HasForeignKey(projectListing => projectListing.GenreName)
            .IsRequired(false);
        
        // Configuring one-to-many relationship: Listing has many MusicianSlots
        modelBuilder.Entity<Listing>()
            .HasMany(listing => listing.MusicianSlots)
            .WithOne(slot => slot.Listing)
            .HasForeignKey(slot => slot.ListingId);

        // Many-to-One relationship: MusicianSlot has an optional assignee Account
        modelBuilder.Entity<MusicianSlot>()
            .HasOne(slot => slot.Assignee)
            .WithMany(account => account.AssignedMusicianSlots)
            .HasForeignKey(slot => slot.AssigneeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Configuring many-to-many relationship between Account and MusicianRole
        modelBuilder.Entity<Account>()
            .HasMany(account => account.MusicianRoles)
            .WithMany(role => role.Accounts);
        
        // Many-to-Many relationship: Account <-> Chatroom
        modelBuilder.Entity<Account>()
            .HasMany(account => account.Chatrooms)
            .WithMany(chatroom => chatroom.Members);
        
        // Many-to-One relationship: Chatroom has one owner (Account)
        modelBuilder.Entity<Chatroom>()
            .HasOne(chatroom => chatroom.Owner)
            .WithMany()
            .HasForeignKey(chatroom => chatroom.OwnerId);

        // One-to-Many relationship: Chatroom -> Message
        modelBuilder.Entity<Chatroom>()
            .HasMany(chatroom => chatroom.Messages)
            .WithOne(message => message.Chatroom)
            .HasForeignKey(message => message.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);  // When a chatroom is deleted, its messages are also deleted

        // One-to-Many relationship: Account -> Message
        modelBuilder.Entity<Account>()
            .HasMany<Message>()
            .WithOne(message => message.Sender)
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.SetNull); // When an account is deleted, their messages are not automatically deleted
        
        PopulateGenres(modelBuilder);
        PopulateMusicianRole(modelBuilder);
    }
    
    private void PopulateGenres(ModelBuilder modelBuilder)
    {
        List<Genre> defaultMusicianRoles =
        [
            new() { Name = "Pop" },
            new() { Name = "Hip-Hop" },
            new() { Name = "Rap" },
            new() { Name = "Rock" },
            new() { Name = "Indie" },
            new() { Name = "Electronic" },
            new() { Name = "Dance" },
            new() { Name = "R&B" },
            new() { Name = "Soul" },
            new() { Name = "Jazz" },
            new() { Name = "Classical" },
            new() { Name = "Metal" },
            new() { Name = "Punk" },
            new() { Name = "Reggae" },
            new() { Name = "Funk" },
            new() { Name = "Blues" },
            new() { Name = "Country" },
            new() { Name = "K-Pop" },
            new() { Name = "Folk" },
            new() { Name = "Edm" },
            new() { Name = "Trap" },
            new() { Name = "Ambient" },
            new() { Name = "House" },
            new() { Name = "Techno" },
            new() { Name = "Dubstep" },
            new() { Name = "Grunge" },
            new() { Name = "Synthwave" }
        ];
        
        modelBuilder.Entity<Genre>().HasData(defaultMusicianRoles);
    }

    private void PopulateMusicianRole(ModelBuilder modelBuilder)
    {
        List<MusicianRole> defaultMusicianRoles =
        [
            new() { Name = "Songwriter" },
            new() { Name = "Vocalist" },
            new() { Name = "Guitarist" },
            new() { Name = "Bassist" },
            new() { Name = "Drummer" },
            new() { Name = "Keyboardist" },
            new() { Name = "Pianist" },
            new() { Name = "Trumpeter" },
            new() { Name = "Violinist" },
            new() { Name = "Synthesizer" },
            new() { Name = "Sampler" },
            new() { Name = "Sound Engineer" },
            new() { Name = "Producer" },
            new() { Name = "Acoustic Guitarist" },
            new() { Name = "Mixing Engineer" },
            new() { Name = "Mastering Engineer" },
        ];
        
        modelBuilder.Entity<MusicianRole>().HasData(defaultMusicianRoles);
    }
}