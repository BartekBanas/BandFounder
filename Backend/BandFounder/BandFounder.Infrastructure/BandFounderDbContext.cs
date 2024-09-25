using BandFounder.Domain.Entities;
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
    }
}