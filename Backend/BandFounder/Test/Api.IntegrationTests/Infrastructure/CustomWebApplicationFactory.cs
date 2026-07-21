using AspNetCoreRateLimit;
using BandFounder.Application.Services.Email;
using BandFounder.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public RecordingEmailSender EmailSender { get; } = new();

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:BandfounderDatabase", _connectionString);
        builder.UseSetting("JwtConfiguration:Issuer", "Bandfounder");
        builder.UseSetting("JwtConfiguration:Audience", "Bandfounder");
        builder.UseSetting("JwtConfiguration:SecretKey", "a4336941e0769d65e0b56415d58c20c6");
        builder.UseSetting("JwtConfiguration:Expires", "60");
        builder.UseSetting("FRONTEND_BASE_URL", "http://localhost:5173");
        builder.UseSetting("PASSWORD_RESET_TOKEN_TTL_MINUTES", "15");
        builder.UseSetting("EMAIL_FROM_ADDRESS", "noreply@bandfounder.com");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<BandFounderDbContext>));
            services.RemoveAll(typeof(BandFounderDbContext));
            services.RemoveAll(typeof(IEmailSender));

            services.AddDbContext<BandFounderDbContext>(options =>
                options.UseNpgsql(_connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("BandFounder.Api")));

            services.AddSingleton<IEmailSender>(EmailSender);

            services.Configure<IpRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.GeneralRules = [];
            });
        });
    }
}
