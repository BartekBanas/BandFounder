using System.Text.Json;
using BandFounder.Application.Dtos;

namespace BandFounder.Application.Services;

public class SpotifyContentService
{
    public static async Task<SpotifyTopArtistsResponse> GetTopArtistsAsync()
    {
        var tokenService = new SpotifyAccessTokenService();
        var accessToken = await tokenService.GetAccessTokenAsync();
        const string url = "https://api.spotify.com/v1/me/top/artists?limit=5";

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<SpotifyTopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();
    }
}