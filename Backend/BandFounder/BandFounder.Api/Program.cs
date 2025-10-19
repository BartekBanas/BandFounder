using System.Text.Json.Serialization;
using BandFounder.Api.Controllers;
using BandFounder.Api.Extensions;
using BandFounder.Api.Middlewares;
using BandFounder.Api.WebSockets;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Authorization;
using BandFounder.Application.Services.Authorization.Handlers;
using BandFounder.Application.Services.Authorization.Requirements;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
var services = builder.Services;

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

// services.Configure<IdentityOptions>(options =>
// {
//     options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
//     options.Lockout.MaxFailedAccessAttempts = 5;
//     options.Lockout.AllowedForNewUsers = true;
// });

services.Configure<JwtConfiguration>(configuration.GetSection(nameof(JwtConfiguration)));

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
services.AddScoped<IRepository<Artist>, Repository<Artist, BandFounderDbContext>>();
services.AddScoped<IRepository<Genre>, Repository<Genre, BandFounderDbContext>>();
services.AddScoped<IRepository<SpotifyTokens>, Repository<SpotifyTokens, BandFounderDbContext>>();

services.AddScoped<IRepository<ProfilePicture>, Repository<ProfilePicture, BandFounderDbContext>>();
services.AddScoped<IRepository<MusicianRole>, Repository<MusicianRole, BandFounderDbContext>>();
services.AddScoped<IRepository<MusicianSlot>, Repository<MusicianSlot, BandFounderDbContext>>();
services.AddScoped<IRepository<Listing>, Repository<Listing, BandFounderDbContext>>();

services.AddScoped<IHashingService, HashingService>();

services.AddScoped<IAccountService, AccountService>();
services.AddScoped<IMessageService, MessageService>();
services.AddScoped<IChatroomService, ChatroomService>();
services.AddScoped<ISpotifyConnectionService, SpotifyConnectionService>();
services.AddScoped<ISpotifyClient, SpotifyClient>();
services.AddScoped<IMusicTasteService, MusicTasteService>();
services.AddScoped<IListingService, ListingService>();
services.AddScoped<IContentService, ContentService>();

services.AddSingleton<WebSocketConnectionManager>();

services.AddScoped<ErrorHandlingMiddleware>();

services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

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

// app.Services.CreateScope().ServiceProvider.GetRequiredService<BandFounderDbContext>().Database.EnsureDeleted();
app.Services.CreateScope().ServiceProvider.GetRequiredService<BandFounderDbContext>().Database.EnsureCreated();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(policyBuilder => policyBuilder
    .AllowAnyHeader()
    .WithOrigins("http://localhost:3000", "http://localhost:3001")
    .AllowAnyMethod()
    .AllowCredentials());

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();