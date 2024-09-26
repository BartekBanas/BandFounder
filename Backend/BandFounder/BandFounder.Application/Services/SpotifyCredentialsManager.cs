using System.Text.Json;
using BandFounder.Application.Dtos;

namespace BandFounder.Application.Services;

public class SpotifyCredentialsManager
{
    public  SpotifyCredentials SpotifyCredentials { get; private set; }
    private readonly string _filePath;

    public SpotifyCredentialsManager(string filePath = "./spotifyAppCredentials.json")
    {
        _filePath = filePath;
    }

    public async Task LoadCredentials()
    {
        try
        {
            var data = await File.ReadAllTextAsync(_filePath);
            SpotifyCredentials = JsonSerializer.Deserialize<SpotifyCredentials>(data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading config file: {ex.Message}", ex);
        }
    }
}