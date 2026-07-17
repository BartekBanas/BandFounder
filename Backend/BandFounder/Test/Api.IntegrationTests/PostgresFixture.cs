using Testcontainers.PostgreSql;

namespace Api.IntegrationTests;

[SetUpFixture]
public class PostgresFixture
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container has not been started.");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("bandfounder_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}