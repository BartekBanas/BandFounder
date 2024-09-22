namespace Bandfounder.Api;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class SpotifyAppConfigManager
{
    public  Config Config { get; private set; }
    private readonly string _filePath;

    public SpotifyAppConfigManager(string filePath = "./spotifyAppConfig.json")
    {
        _filePath = filePath;
    }

    public async Task LoadConfigAsync()
    {
        try
        {
            var data = await File.ReadAllTextAsync(_filePath);
            Config = JsonSerializer.Deserialize<Config>(data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading config file: {ex.Message}", ex);
        }
    }
}