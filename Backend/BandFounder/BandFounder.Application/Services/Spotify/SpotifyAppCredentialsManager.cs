using System.Text.Json;
using BandFounder.Application.Dtos.Spotify;

namespace BandFounder.Application.Services.Spotify;

public class SpotifyAppCredentialsManager
{
    private readonly string _filePath;

    public SpotifyAppCredentialsManager(string filePath = "./spotifyAppCredentials.json")
    {
        _filePath = filePath;
    }

    public async Task<SpotifyAppCredentials> LoadCredentials()
    {
        try
        {
            var data = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<SpotifyAppCredentials>(data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading config file: {ex.Message}", ex);
        }
    }
}