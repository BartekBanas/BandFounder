using AspNetCoreRateLimit;
using BandFounder.Application.Services;
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
    private readonly bool _trustedBackupMaintenanceEnabled;
    public RecordingEmailSender EmailSender { get; } = new();
    public ControllableMessageEmailNotificationGate NotificationGate { get; } = new();

    public CustomWebApplicationFactory(
        string connectionString,
        bool trustedBackupMaintenanceEnabled = false)
    {
        _connectionString = connectionString;
        _trustedBackupMaintenanceEnabled = trustedBackupMaintenanceEnabled;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:BandfounderDatabase", _connectionString);
        builder.UseSetting("JwtConfiguration:Issuer", "Bandfounder");
        builder.UseSetting("JwtConfiguration:Audience", "Bandfounder");
        builder.UseSetting("JwtConfiguration:SecretKey", "a4336941e0769d65e0b56415d58c20c6");
        builder.UseSetting("JwtConfiguration:Expires", "60");
        builder.UseSetting("FRONTEND_BASE_URL", "http://127.0.0.1:3000");
        builder.UseSetting("PASSWORD_RESET_TOKEN_TTL_MINUTES", "15");
        builder.UseSetting("EMAIL_FROM_ADDRESS", "noreply@bandfounder.com");
        builder.UseSetting("MessageEmailNotifications:PollIntervalSeconds", "1");
        builder.UseSetting("MessageEmailNotifications:MaxAttempts", "2");
        builder.UseSetting("MessageEmailNotifications:MaxSnippetLength", "160");
        builder.UseSetting("MessageEmailNotifications:StaleClaimMinutes", "15");
        builder.UseSetting(
            "Backup:TrustedMaintenanceEnabled",
            _trustedBackupMaintenanceEnabled.ToString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<BandFounderDbContext>));
            services.RemoveAll(typeof(BandFounderDbContext));
            services.RemoveAll(typeof(IEmailSender));
            services.RemoveAll(typeof(IMessageEmailNotificationGate));

            services.AddDbContext<BandFounderDbContext>(options =>
                options.UseNpgsql(_connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("BandFounder.Api")));

            services.AddSingleton<IEmailSender>(EmailSender);
            services.AddSingleton<IMessageEmailNotificationGate>(NotificationGate);

            services.Configure<IpRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.GeneralRules = [];
            });
        });
    }
}
