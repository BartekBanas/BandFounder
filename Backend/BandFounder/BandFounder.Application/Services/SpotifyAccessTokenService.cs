using System.Text;
using System.Text.Json;
using BandFounder.Application.Dtos;

namespace BandFounder.Application.Services;

public class SpotifyAccessTokenService
{
    private const string StoreFilePath = "./spotifyTempTokenStorage.json";

    public async Task SaveAccessTokenAsync(string accessToken, string refreshToken, int durationInSeconds)
    {
        var tokenData = new SpotifyTokenStorage
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpirationDate = DateTime.UtcNow.AddSeconds(durationInSeconds - 100)
        };

        var json = JsonSerializer.Serialize(tokenData);
        await File.WriteAllTextAsync(StoreFilePath, json);
    }

    private async Task SaveRefreshedAccessTokenAsync(string accessToken, int durationInSeconds)
    {
        // Load the previously saved token data to preserve the refresh token
        var spotifyTokenStorage = await GetAccessTokenFromFileAsync();

        if (spotifyTokenStorage == null)
        {
            throw new InvalidOperationException("No token data found to update.");
        }

        // Update only the access token and expiration date, keep the same refresh token
        spotifyTokenStorage.AccessToken = accessToken;
        spotifyTokenStorage.ExpirationDate = DateTime.UtcNow.AddSeconds(durationInSeconds - 100);

        // Serialize and save the updated token data
        var json = JsonSerializer.Serialize(spotifyTokenStorage);
        await File.WriteAllTextAsync(StoreFilePath, json);
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var tokenData = await GetAccessTokenFromFileAsync();

        // Check if the stored token is still valid
        if (DateTime.UtcNow < tokenData.ExpirationDate)
        {
            return tokenData.AccessToken;
        }
        else
        {
            return await RefreshTokenAsync(tokenData.RefreshToken);
        }
    }

    private async Task<string> RefreshTokenAsync(string refreshToken)
    {
        var spotifyAppCredentials = await new SpotifyAppCredentialsManager().LoadCredentials();

        const string url = "https://accounts.spotify.com/api/token";
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        var authenticationHeaderValue =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));
        request.Headers.Add("Authorization", $"Basic {authenticationHeaderValue}");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        request.Content = content;

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var spotifyAccessCredentials = JsonSerializer.Deserialize<SpotifyTokenRefreshResponse>(responseContent);

        if (spotifyAccessCredentials is null)
        {
            throw new Exception(); // TODO get more creative
        }

        await SaveRefreshedAccessTokenAsync(spotifyAccessCredentials.AccessToken, spotifyAccessCredentials.Duration);
        
        return spotifyAccessCredentials.AccessToken;
    }

    private async Task<SpotifyTokenStorage> GetAccessTokenFromFileAsync()
    {
        if (!File.Exists(StoreFilePath))
        {
            throw new Exception();
        }

        var json = await File.ReadAllTextAsync(StoreFilePath);
        var tokenData = JsonSerializer.Deserialize<SpotifyTokenStorage>(json);

        return tokenData ?? throw new InvalidOperationException();
    }
}