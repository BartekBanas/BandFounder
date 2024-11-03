using BandFounder.Api.Controllers;
using BandFounder.Api.Extensions;
using BandFounder.Application.Error;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Authorization;
using BandFounder.Application.Services.Authorization.Handlers;
using BandFounder.Application.Services.Authorization.Requirements;
using BandFounder.Application.Services.Jwt;
using BandFounder.Application.Services.Spotify;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
var services = builder.Services;

services.AddControllers().AddApplicationPart(typeof(ControllerAssemblyMarker).Assembly);
services.AddEndpointsApiExplorer();
services.AddHttpContextAccessor();
services.AddSwaggerGen();

builder.Services.AddDbContext<BandFounderDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("BandfounderDatabase")));

services.Configure<JwtConfiguration>(configuration.GetSection(nameof(JwtConfiguration)));

services.AddScoped<IAuthorizationHandler, ChatRoomAuthorizationHandler>();
services.AddScoped<IAuthorizationHandler, AccountAuthorizationHandler>();

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

services.AddValidatorsFromAssembly(typeof(BandFounder.Application.Validation.AssemblyMarker).Assembly);

services.AddScoped<IJwtService, JwtService>();
services.AddScoped<IAuthenticationService, AuthenticationService>();

services.AddScoped<IRepository<Account>, Repository<Account, BandFounderDbContext>>();
services.AddScoped<IRepository<Message>, Repository<Message, BandFounderDbContext>>();
services.AddScoped<IRepository<Chatroom>, Repository<Chatroom, BandFounderDbContext>>();
services.AddScoped<IRepository<Artist>, Repository<Artist, BandFounderDbContext>>();
services.AddScoped<IRepository<Genre>, Repository<Genre, BandFounderDbContext>>();
services.AddScoped<IRepository<SpotifyTokens>, Repository<SpotifyTokens, BandFounderDbContext>>();

services.AddScoped<IRepository<MusicianRole>, Repository<MusicianRole, BandFounderDbContext>>();
services.AddScoped<IRepository<MusicianSlot>, Repository<MusicianSlot, BandFounderDbContext>>();
services.AddScoped<IRepository<Listing>, Repository<Listing, BandFounderDbContext>>();

services.AddScoped<IHashingService, HashingService>();

services.AddScoped<IAccountService, AccountService>();
services.AddScoped<IMessageService, MessageService>();
services.AddScoped<IChatroomService, ChatroomService>();
services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();
services.AddScoped<ISpotifyContentRetriever, SpotifyContentRetriever>();
services.AddScoped<ISpotifyContentManager, SpotifyContentManager>();
services.AddScoped<IMusicTasteService, MusicTasteService>();
services.AddScoped<IListingService, ListingService>();

services.AddScoped<ErrorHandlingMiddleware>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

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
    .WithOrigins("http://localhost:3000")
    .AllowAnyMethod()
    .AllowCredentials());

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();