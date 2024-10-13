using BandFounder.Domain.Entities;
using BandFounder.Domain.Entities.Spotify;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public class BandFounderDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<SpotifyCredentials> SpotifyCredentials { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Genre> Genres { get; set; }
    
    public DbSet<MusicianRole> MusicianRoles { get; set; }
    public DbSet<MusicProjectListing> MusicProjectListings { get; set; }
    public DbSet<MusicianSlot> MusicianSlots { get; set; }
    
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
            .HasForeignKey(slot => slot.Listing.Id);

        // Configuring one-to-many relationship: MusicianSlot has one MusicianRole
        modelBuilder.Entity<MusicianSlot>()
            .HasOne(slot => slot.Role)
            .WithMany()
            .HasForeignKey(slot => slot.Role.Id);

        // Configuring many-to-many relationship between Account and MusicianRole
        modelBuilder.Entity<Account>()
            .HasMany(account => account.MusicRoles)
            .WithMany(role => role.Accounts);
        // .UsingEntity<Dictionary<string, object>>(
        //     "AccountMusicRole",
        //     j => j.HasOne<MusicianRole>().WithMany().HasForeignKey("MusicRoleId"),
        //     j => j.HasOne<Account>().WithMany().HasForeignKey("AccountId"));
    }
}