using System.Text.Json;
using BandFounder.Application.Dtos.Spotify;

namespace BandFounder.Application.Services.Spotify;

public interface ISpotifyContentRetriever
{
    Task<List<ArtistDto>> GetTopArtistsAsync();
    Task<List<ArtistDto>> GetFollowedArtistsAsync();
}

public class SpotifyContentRetriever : ISpotifyContentRetriever
{
    private const string SpotifyTopArtistsUrl = "https://api.spotify.com/v1/me/top/artists?limit=50";
    private const string SpotifyFollowedArtistsUrl = "https://api.spotify.com/v1/me/following?type=artist";
    
    private readonly ISpotifyCredentialsService _credentialsService;

    public SpotifyContentRetriever(ISpotifyCredentialsService credentialsService)
    {
        _credentialsService = credentialsService;
    }

    public async Task<List<ArtistDto>> GetTopArtistsAsync()
    {
        var accessToken = await _credentialsService.GetAccessTokenAsync();
        
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, SpotifyTopArtistsUrl);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        var responseDto = JsonSerializer.Deserialize<TopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();
        
        return responseDto.Items;
    }
    
    public async Task<List<ArtistDto>> GetFollowedArtistsAsync()
    {
        var accessToken = await _credentialsService.GetAccessTokenAsync();
        
        var url = SpotifyFollowedArtistsUrl;
        var followedArtists = new List<ArtistDto>();

        using var client = new HttpClient();
        do
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseDto = JsonSerializer.Deserialize<FollowedArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

            followedArtists.AddRange(responseDto.Artists.Items);

            url = responseDto.Artists.Next;

        } while (!string.IsNullOrEmpty(url));

        return followedArtists;
    }
}