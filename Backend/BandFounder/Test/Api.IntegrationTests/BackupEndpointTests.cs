using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Backup;

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
    public async Task BackupEndpoints_WhenTrustedMaintenanceIsEnabled_RoundTripAccount()
    {
        var restore = await Client.PostAsJsonAsync("/api/backup", new
        {
            accounts = new[]
            {
                new
                {
                    name = "trusted",
                    email = "trusted@example.com",
                    musicianRoles = Array.Empty<string>(),
                    artists = Array.Empty<string>()
                }
            },
            artists = Array.Empty<object>()
        });

        Assert.That(restore.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var export = await Client.GetAsync("/api/backup");

        Assert.That(export.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var backup = await ReadJsonAsync<BackupDto>(export);
        Assert.That(backup.Accounts.Single().Email, Is.EqualTo("trusted@example.com"));
    }
}
