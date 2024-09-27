using System.Text.Json;
using BandFounder.Application.Dtos.Spotify;

namespace BandFounder.Application.Services;

public class SpotifyContentService
{
    public static async Task<TopArtistsResponse> GetTopArtistsAsync()
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
        
        return JsonSerializer.Deserialize<TopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();
    }
    
    public static async Task<List<Artist>> GetFollowedArtistsAsync()
    {
        var accessToken = await new SpotifyAccessTokenService().GetAccessTokenAsync();
        var url = "https://api.spotify.com/v1/me/following?type=artist";
        var allArtists = new List<Artist>();

        using var client = new HttpClient();
        do
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseDto = JsonSerializer.Deserialize<SpotifyFollowedArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

            allArtists.AddRange(responseDto.Artists.Items);

            url = responseDto.Artists.Next;

        } while (!string.IsNullOrEmpty(url));

        return allArtists;
    }
}