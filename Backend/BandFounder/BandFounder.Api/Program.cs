using System.Text.Json.Serialization;
using BandFounder.Api.BackgroundServices;
using BandFounder.Api.Controllers;
using BandFounder.Api.Extensions;
using BandFounder.Api.Middlewares;
using BandFounder.Api.WebSockets;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Authorization;
using BandFounder.Application.Services.Authorization.Handlers;
using BandFounder.Application.Services.Authorization.Requirements;
using BandFounder.Application.Services.Email;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AspNetCoreRateLimit;
using Resend;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
var services = builder.Services;

static int ApplyEnvironmentOverride(IConfiguration configuration, string name, int currentValue)
{
    var rawValue = configuration[name] ?? Environment.GetEnvironmentVariable(name);
    if (rawValue is null)
    {
        return currentValue;
    }

    return int.TryParse(rawValue, out var parsedValue) ? parsedValue : 0;
}

services.AddControllers().AddApplicationPart(typeof(ControllerAssemblyMarker).Assembly).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
services.AddEndpointsApiExplorer();
services.AddHttpContextAccessor();
services.AddSwaggerGen();

services.AddDbContext<BandFounderDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("BandfounderDatabase"), 
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("BandFounder.Api")));

services.Configure<JwtConfiguration>(configuration.GetSection(nameof(JwtConfiguration)));

var resendApiKey = configuration["RESEND_API_KEY"]
                   ?? Environment.GetEnvironmentVariable("RESEND_API_KEY")
                   ?? string.Empty;

services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

services.AddOptions<MessageEmailNotificationOptions>()
    .Bind(configuration.GetSection(MessageEmailNotificationOptions.SectionName))
    .PostConfigure(options =>
    {
        options.PollIntervalSeconds = ApplyEnvironmentOverride(
            configuration,
            "MESSAGE_EMAIL_POLL_INTERVAL_SECONDS",
            options.PollIntervalSeconds);
        options.MaxAttempts = ApplyEnvironmentOverride(
            configuration,
            "MESSAGE_EMAIL_MAX_ATTEMPTS",
            options.MaxAttempts);
    })
    .Validate(
        options => options.PollIntervalSeconds > 0 &&
                   options.MaxAttempts > 0 &&
                   options.MaxSnippetLength > 0 &&
                   options.StaleClaimMinutes > 0,
        "Message email notification worker values must all be positive.")
    .ValidateOnStart();

services.PostConfigure<EmailOptions>(options =>
{
    options.FromAddress = configuration["EMAIL_FROM_ADDRESS"]
                          ?? Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS")
                          ?? options.FromAddress;

    options.FrontendBaseUrl = configuration["FRONTEND_BASE_URL"]
                              ?? Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")
                              ?? options.FrontendBaseUrl;

    var ttlRaw = configuration["PASSWORD_RESET_TOKEN_TTL_MINUTES"]
                 ?? Environment.GetEnvironmentVariable("PASSWORD_RESET_TOKEN_TTL_MINUTES");
    if (int.TryParse(ttlRaw, out var ttlMinutes) && ttlMinutes > 0)
    {
        options.PasswordResetTokenTtlMinutes = ttlMinutes;
    }

    var verificationTtlRaw = configuration["EMAIL_VERIFICATION_TOKEN_TTL_MINUTES"]
                             ?? Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_TOKEN_TTL_MINUTES");
    if (int.TryParse(verificationTtlRaw, out var verificationTtlMinutes) && verificationTtlMinutes > 0)
    {
        options.EmailVerificationTokenTtlMinutes = verificationTtlMinutes;
    }

    var cooldownRaw = configuration["EMAIL_VERIFICATION_RESEND_COOLDOWN_SECONDS"]
                      ?? Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_RESEND_COOLDOWN_SECONDS");
    if (int.TryParse(cooldownRaw, out var cooldownSeconds) && cooldownSeconds > 0)
    {
        options.EmailVerificationResendCooldownSeconds = cooldownSeconds;
    }
});

var isDevelopmentOrTesting = builder.Environment.IsDevelopment()
                             || builder.Environment.IsEnvironment("Testing");
if (string.IsNullOrWhiteSpace(resendApiKey) && !isDevelopmentOrTesting)
{
    throw new InvalidOperationException(
        "RESEND_API_KEY is required outside Development and Testing environments.");
}

services.AddHttpClient<ResendClient>();
services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = resendApiKey;
});
services.AddTransient<IResend, ResendClient>();
if (isDevelopmentOrTesting && string.IsNullOrWhiteSpace(resendApiKey))
{
    services.AddScoped<IEmailSender, LoggingEmailSender>();
}
else
{
    services.AddScoped<IEmailSender, ResendEmailSender>();
}
services.AddScoped<IPasswordResetTokenStore, PasswordResetTokenStore>();
services.AddScoped<IEmailVerificationStore, EmailVerificationTokenStore>();
services.AddScoped<IUnitOfWork, UnitOfWork>();

services.AddScoped<IAuthorizationHandler, ChatRoomAuthorizationHandler>();
services.AddScoped<IAuthorizationHandler, AccountAuthorizationHandler>();
services.AddScoped<IAuthorizationHandler, ListingAuthorizationHandler>();

services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.IsMemberOf, policy =>
        policy.Requirements.Add(new IsMemberOfRequirement()));

    options.AddPolicy(AuthorizationPolicies.IsOwnerOf, policy =>
        policy.Requirements.Add(new IsOwnerRequirement()));
});

var jwtConfig = configuration.GetRequiredSection("JwtConfiguration").Get<JwtConfiguration>();
services.AddJwtAuthentication(jwtConfig!);
services.AddAuthorizationSwaggerGen();

services.AddValidatorsFromAssembly(typeof(BandFounder.Domain.Validation.AssemblyMarker).Assembly);

services.AddScoped<IJwtService, JwtService>();
services.AddScoped<IAuthenticationService, AuthenticationService>();

services.AddScoped<IRepository<Account>, Repository<Account, BandFounderDbContext>>();
services.AddScoped<IRepository<Message>, Repository<Message, BandFounderDbContext>>();
services.AddScoped<IRepository<Chatroom>, Repository<Chatroom, BandFounderDbContext>>();
services.AddScoped<IRepository<ChatroomReadState>, Repository<ChatroomReadState, BandFounderDbContext>>();
services.AddScoped<IRepository<Artist>, Repository<Artist, BandFounderDbContext>>();
services.AddScoped<IRepository<Genre>, Repository<Genre, BandFounderDbContext>>();
services.AddScoped<IRepository<SpotifyTokens>, Repository<SpotifyTokens, BandFounderDbContext>>();
services.AddScoped<IRepository<PasswordResetToken>, Repository<PasswordResetToken, BandFounderDbContext>>();
services.AddScoped<IRepository<AccountNotificationPreferences>, Repository<AccountNotificationPreferences, BandFounderDbContext>>();
services.AddScoped<IEmailNotificationOutboxRepository, EmailNotificationOutboxRepository>();

services.AddScoped<IRepository<ProfilePicture>, Repository<ProfilePicture, BandFounderDbContext>>();
services.AddScoped<IRepository<MusicianRole>, Repository<MusicianRole, BandFounderDbContext>>();
services.AddScoped<IRepository<MusicianSlot>, Repository<MusicianSlot, BandFounderDbContext>>();
services.AddScoped<IRepository<Listing>, Repository<Listing, BandFounderDbContext>>();

services.AddScoped<IHashingService, HashingService>();

services.AddScoped<IAccountService, AccountService>();
services.AddScoped<IMessageService, MessageService>();
services.AddSingleton<IMessageEmailNotificationGate, NoOpMessageEmailNotificationGate>();
services.AddScoped<IMessageEmailNotificationService, MessageEmailNotificationService>();
services.AddScoped<IEmailVerificationService, EmailVerificationService>();
services.AddScoped<IChatroomService, ChatroomService>();
services.AddScoped<ISpotifyConnectionService, SpotifyConnectionService>();
services.AddScoped<ISpotifyClient, SpotifyClient>();
services.AddScoped<ISpotifyAppCredentialsService, SpotifyAppCredentialsService>();
services.AddScoped<IMusicTasteService, MusicTasteService>();
services.AddScoped<IListingService, ListingService>();
services.AddScoped<IContentService, ContentService>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    services.AddHostedService<MessageEmailNotificationWorker>();
}

services.AddSingleton<WebSocketConnectionManager>();

services.AddScoped<ErrorHandlingMiddleware>();

services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

services.AddCorsPolicies(configuration);

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors(app.Environment.IsDevelopment() ? CorsPolicies.LocalDevelopment : CorsPolicies.Production);

app.UseIpRateLimiting();

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var connectionManager = app.Services.GetRequiredService<WebSocketConnectionManager>();
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await connectionManager.HandleWebSocketConnectionAsync(context, webSocket);
    }
    else
    {
        await next();
    }
});

var isTesting = app.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    // app.Services.CreateScope().ServiceProvider.GetRequiredService<BandFounderDbContext>().Database.EnsureDeleted();
    app.Services.CreateScope().ServiceProvider.GetRequiredService<BandFounderDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!isTesting)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
