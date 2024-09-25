using BandFounder.Api.Controllers;
using BandFounder.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
var services = builder.Services;

services.AddControllers().AddApplicationPart(typeof(ControllerAssemblyMarker).Assembly);
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

builder.Services.AddDbContext<BandFounderDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("BandfounderDatabase")));

var app = builder.Build();

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

// app.UseAuthentication();

// app.UseAuthorization();

app.MapControllers();

app.Run();