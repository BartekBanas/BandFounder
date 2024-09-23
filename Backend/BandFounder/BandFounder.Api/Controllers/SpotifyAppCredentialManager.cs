using System.Text.Json;

namespace BandFounder.Api.Controllers;

public class SpotifyAppCredentialManager
{
    public  SpotifyAppCredentials SpotifyAppCredentials { get; private set; }
    private readonly string _filePath;

    public SpotifyAppCredentialManager(string filePath = "./spotifyAppCredentials.json")
    {
        _filePath = filePath;
    }

    public async Task LoadCredentials()
    {
        try
        {
            var data = await File.ReadAllTextAsync(_filePath);
            SpotifyAppCredentials = JsonSerializer.Deserialize<SpotifyAppCredentials>(data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading config file: {ex.Message}", ex);
        }
    }
}