using BandFounder.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddApplicationPart(typeof(ControllerAssemblyMarker).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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