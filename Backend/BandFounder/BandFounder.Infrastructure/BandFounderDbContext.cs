using BandFounder.Domain.Entities;
using BandFounder.Domain.Entities.Spotify;
using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public class BandFounderDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<SpotifyCredentials> SpotifyCredentials { get; set; }
    
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
    }
}