using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BandFounder.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private DbConnection _connection = null!;

    private CustomWebApplicationFactory _factory = null!;
    private Respawner _respawner = null!;

    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task IntegrationOneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory(PostgresFixture.ConnectionString);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        await dbContext.Database.MigrateAsync();

        _connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("Genres"),
                new Table("MusicianRoles")
            ]
        });
    }

    [SetUp]
    public async Task IntegrationSetUp()
    {
        await _respawner.ResetAsync(_connection);
        Client.DefaultRequestHeaders.Authorization = null;
    }

    [OneTimeTearDown]
    public async Task IntegrationOneTimeTearDown()
    {
        await _connection.DisposeAsync();
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected void AuthenticateAs(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<string> RegisterAsync(string name, string email, string password = "Password123!")
    {
        var response = await Client.PostAsJsonAsync("/api/accounts", new
        {
            name,
            email,
            password
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Register failed ({response.StatusCode}): {body}");
        var token = UnwrapToken(body);
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        return token;
    }

    protected async Task<string> AuthenticateAsync(string usernameOrEmail, string password = "Password123!")
    {
        var response = await Client.PostAsJsonAsync("/api/accounts/authenticate", new
        {
            usernameOrEmail,
            password
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Authenticate failed ({response.StatusCode}): {body}");
        var token = UnwrapToken(body);
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        return token;
    }

    private static string UnwrapToken(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length >= 2 && trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            return JsonSerializer.Deserialize<string>(trimmed) ?? trimmed;

        return trimmed;
    }

    protected async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.That(payload, Is.Not.Null);
        return payload!;
    }
}