using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Backup;
using BandFounder.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[TestFixture]
public class BackupEndpointDisabledTests : IntegrationTestBase
{
    [TestCase("GET")]
    [TestCase("POST")]
    public async Task BackupEndpoint_WhenTrustedMaintenanceIsDisabled_ReturnsNotFound(
        string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/backup");
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new
            {
                accounts = Array.Empty<object>(),
                artists = Array.Empty<object>()
            });
        }

        var response = await Client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

[TestFixture]
public class BackupEndpointTrustedMaintenanceTests : IntegrationTestBase
{
    protected override bool TrustedBackupMaintenanceEnabled => true;

    [Test]
    public async Task RestoreBackup_WithVerificationTimestamp_PreservesTrustedValue()
    {
        var verifiedAt = new DateTime(2026, 8, 23, 20, 15, 0, DateTimeKind.Utc);

        var restoreResponse = await RestoreAccountAsync("verified", "verified@example.com", verifiedAt);

        Assert.That(restoreResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var account = await dbContext.Accounts.SingleAsync(
            candidate => candidate.Email == "verified@example.com");
        Assert.That(account.EmailVerifiedAt, Is.EqualTo(verifiedAt));

        var backupResponse = await Client.GetAsync("/api/backup");
        Assert.That(backupResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var backup = await ReadJsonAsync<BackupDto>(backupResponse);
        Assert.That(backup.Accounts.Single().EmailVerifiedAt, Is.EqualTo(verifiedAt));
    }

    [Test]
    public async Task RestoreBackup_WithoutVerificationTimestamp_RestoresUnverified()
    {
        var restoreResponse = await RestoreAccountAsync(
            "unverified",
            "unverified@example.com",
            emailVerifiedAt: null);

        Assert.That(restoreResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var account = await dbContext.Accounts.SingleAsync(
            candidate => candidate.Email == "unverified@example.com");
        Assert.That(account.EmailVerifiedAt, Is.Null);
    }

    private Task<HttpResponseMessage> RestoreAccountAsync(
        string name,
        string email,
        DateTime? emailVerifiedAt)
    {
        return Client.PostAsJsonAsync("/api/backup", new
        {
            accounts = new[]
            {
                new
                {
                    name,
                    email,
                    emailVerifiedAt,
                    musicianRoles = Array.Empty<string>(),
                    artists = Array.Empty<string>()
                }
            },
            artists = Array.Empty<object>()
        });
    }
}
