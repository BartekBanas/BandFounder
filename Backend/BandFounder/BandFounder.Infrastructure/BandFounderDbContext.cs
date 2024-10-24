using BandFounder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public class BandFounderDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Chatroom> Chatrooms { get; set; }
    public DbSet<Account> Messages { get; set; }
    public DbSet<SpotifyCredentials> SpotifyCredentials { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Genre> Genres { get; set; }
    
    public DbSet<MusicianRole> MusicianRoles { get; set; }
    public DbSet<MusicianSlot> MusicianSlots { get; set; }
    public DbSet<MusicProjectListing> MusicProjectListings { get; set; }
    
    public BandFounderDbContext(DbContextOptions options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuring relationships
        modelBuilder.Entity<Account>()
            .HasOne(account => account.SpotifyCredentials)
            .WithOne(spotifyCredentials => spotifyCredentials.Account)
            .HasForeignKey<SpotifyCredentials>(spotifyCredentials => spotifyCredentials.AccountId);
        
        // Configuring many-to-many relationship between Account and Artist
        modelBuilder.Entity<Account>()
            .HasMany(account => account.Artists)
            .WithMany(artist => artist.Accounts);
        
        modelBuilder.Entity<Artist>()
            .HasMany(artist => artist.Genres)
            .WithMany(genre => genre.Artists);
        
        // Many-to-One relationship: MusicCollaboration has one owner (Account)
        modelBuilder.Entity<MusicProjectListing>()
            .HasOne(projectListing => projectListing.Owner)
            .WithMany(account => account.MusicProjectListings)
            .HasForeignKey(projectListing => projectListing.AccountId);

        // Many-to-One relationship: MusicCollaboration has an optional Genre
        modelBuilder.Entity<MusicProjectListing>()
            .HasOne(projectListing => projectListing.Genre)
            .WithMany()
            .HasForeignKey(projectListing => projectListing.GenreName)
            .IsRequired(false);
        
        // Configuring one-to-many relationship: MusicProjectListing has many MusicianSlots
        modelBuilder.Entity<MusicProjectListing>()
            .HasMany(listing => listing.MusicianSlots)
            .WithOne(slot => slot.Listing)
            .HasForeignKey(slot => slot.ListingId);

        // Configuring one-to-many relationship: MusicianSlot has one MusicianRole
        modelBuilder.Entity<MusicianSlot>()
            .HasOne(slot => slot.Role)
            .WithMany()
            .HasForeignKey(slot => slot.RoleId);

        // Configuring many-to-many relationship between Account and MusicianRole
        modelBuilder.Entity<Account>()
            .HasMany(account => account.MusicianRoles)
            .WithMany(role => role.Accounts);
        // .UsingEntity<Dictionary<string, object>>(
        //     "AccountMusicRole",
        //     j => j.HasOne<MusicianRole>().WithMany().HasForeignKey("MusicRoleId"),
        //     j => j.HasOne<Account>().WithMany().HasForeignKey("AccountId"));
        
        // Many-to-Many relationship: Account <-> Chatroom
        modelBuilder.Entity<Account>()
            .HasMany(account => account.Chatrooms)
            .WithMany(chatroom => chatroom.Members);

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
            .OnDelete(DeleteBehavior.Restrict); // When an account is deleted, their sent messages are not automatically deleted
    }
}