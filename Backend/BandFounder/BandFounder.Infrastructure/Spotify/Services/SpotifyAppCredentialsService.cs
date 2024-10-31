using System.Text.Json;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Infrastructure.Spotify.Services;

public class SpotifyAppCredentialsService
{
    private readonly string _filePath;

    public SpotifyAppCredentialsService(string filePath = "./spotifyAppCredentials.json")
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