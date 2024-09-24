using Microsoft.EntityFrameworkCore;

namespace BandFounder.Infrastructure;

public class BandFounderDbContext : DbContext
{
    public BandFounderDbContext(DbContextOptions options) : base(options)
    {
        
    }
}