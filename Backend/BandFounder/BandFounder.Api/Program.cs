using BandFounder.Api.Controllers;
using BandFounder.Api.Extensions;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Jwt;
using BandFounder.Application.Services.Spotify;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Entities.Spotify;
using BandFounder.Infrastructure;
using FluentValidation;
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

var jwtConfig = configuration.GetRequiredSection("JwtConfiguration").Get<JwtConfiguration>();
services.AddJwtAuthentication(jwtConfig!);
services.AddAuthorizationSwaggerGen();

services.AddValidatorsFromAssembly(typeof(BandFounder.Application.Validation.AssemblyMarker).Assembly);

services.AddScoped<IJwtService, JwtService>();
services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();

services.AddScoped<IRepository<Account>, Repository<Account, BandFounderDbContext>>();
services.AddScoped<IRepository<Artist>, Repository<Artist, BandFounderDbContext>>();
services.AddScoped<IRepository<Genre>, Repository<Genre, BandFounderDbContext>>();
services.AddScoped<IRepository<SpotifyCredentials>, Repository<SpotifyCredentials, BandFounderDbContext>>();

services.AddScoped<IHashingService, HashingService>();

services.AddScoped<IAccountService, AccountService>();
services.AddScoped<ISpotifyCredentialsService, SpotifyCredentialsService>();
services.AddScoped<ISpotifyContentService, SpotifyContentService>();
services.AddScoped<ISpotifyContentManager, SpotifyContentManager>();

var app = builder.Build();

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