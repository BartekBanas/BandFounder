using System.Text;
using System.Text.Json;
using BandFounder.Infrastructure.Errors;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyClient
{
    Task<SpotifyTokensResponse> RequestAccessTokenAsync(SpotifyConnectionDto dto, SpotifyAppCredentials spotifyAppCredentials);
    Task<SpotifyTokensResponse> RefreshTokenAsync(string refreshToken, SpotifyAppCredentials spotifyAppCredentials);
    Task<List<SpotifyArtistDto>> GetTopArtistsAsync(string accessToken, int limit);
    Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(string accessToken);
}

public class SpotifyClient : ISpotifyClient
{
    private const string SpotifyAccessTokenUrl = "https://accounts.spotify.com/api/token";
    private const string SpotifyTopArtistsUrl = "https://api.spotify.com/v1/me/top/artists";
    private const string SpotifyFollowedArtistsUrl = "https://api.spotify.com/v1/me/following?type=artist";

    public async Task<SpotifyTokensResponse> RequestAccessTokenAsync(SpotifyConnectionDto dto, SpotifyAppCredentials spotifyAppCredentials)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccessTokenUrl);

        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));
        
        request.Headers.Add("Authorization", $"Basic {authHeader}");

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", dto.AuthorizationCode },
            { "redirect_uri", dto.BaseFrontendAppUrl }
        });

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var spotifyTokens = JsonSerializer.Deserialize<SpotifyTokensResponse>(responseContent);

        if (spotifyTokens is null)
        {
            throw new FailedToFetchSpotifyTokenException("Failed to fetch Spotify token");
        }

        return spotifyTokens;
    }
    
    public async Task<SpotifyTokensResponse> RefreshTokenAsync(string refreshToken, SpotifyAppCredentials spotifyAppCredentials)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccessTokenUrl);

        var authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));
        
        request.Headers.Add("Authorization", $"Basic {authHeader}");

        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        ]);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var spotifyTokens = JsonSerializer.Deserialize<SpotifyTokensResponse>(responseContent);
        
        if (spotifyTokens is null)
        {
            throw new FailedToFetchSpotifyTokenException("Failed to refresh Spotify token");
        }

        return spotifyTokens;
    }
    
    public async Task<List<SpotifyArtistDto>> GetTopArtistsAsync(string accessToken, int limit)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, SpotifyTopArtistsUrl + $"?limit={limit}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        var responseDto = JsonSerializer.Deserialize<TopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();
        
        return responseDto.Items;
    }
    
    public async Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(string accessToken)
    {
        var url = SpotifyFollowedArtistsUrl;
        var followedArtists = new List<SpotifyArtistDto>();
        var artistIds = new HashSet<string>();
        
        const int maxRequests = 10;
        var requestCount = 0;

        using var client = new HttpClient();
        do
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseDto = JsonSerializer.Deserialize<FollowedArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

            followedArtists.AddRange(responseDto.Artists.Items.Where(artist => artistIds.Add(artist.Id)));

            url = responseDto.Artists.Next;
            requestCount++;

        } while (!string.IsNullOrEmpty(url) && requestCount < maxRequests);

        return followedArtists;
    }
}