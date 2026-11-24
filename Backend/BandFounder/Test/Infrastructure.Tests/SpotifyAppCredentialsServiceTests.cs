using BandFounder.Infrastructure.Spotify.Services;

namespace Infrastructure.Tests;

[TestFixture]
public class SpotifyAppCredentialsServiceTests
{
    private string _filePath = null!;

    [SetUp]
    public void SetUp()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"spotify-creds-{Guid.NewGuid()}.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Test]
    public async Task LoadCredentials_ReadsCredentialsFromFile()
    {
        await File.WriteAllTextAsync(_filePath, """{"ClientId":"client","ClientSecret":"secret"}""");
        var service = new SpotifyAppCredentialsService(_filePath);

        var credentials = await service.LoadCredentials();

        Assert.That(credentials.ClientId, Is.EqualTo("client"));
        Assert.That(credentials.ClientSecret, Is.EqualTo("secret"));
    }

    [Test]
    public void LoadCredentials_ThrowsWithInnerException_WhenFileMissing()
    {
        var service = new SpotifyAppCredentialsService(_filePath);

        var exception = Assert.ThrowsAsync<Exception>(() => service.LoadCredentials());

        Assert.That(exception!.Message, Does.StartWith("Error reading config file:"));
        Assert.That(exception.InnerException, Is.Not.Null);
    }

    [Test]
    public void LoadCredentials_Throws_WhenFileContainsInvalidJson()
    {
        File.WriteAllText(_filePath, "not-json");
        var service = new SpotifyAppCredentialsService(_filePath);

        Assert.ThrowsAsync<Exception>(() => service.LoadCredentials());
    }
}
