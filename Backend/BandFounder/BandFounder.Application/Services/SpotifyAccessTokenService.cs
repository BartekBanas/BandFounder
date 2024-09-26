namespace BandFounder.Application.Services;

public class SpotifyAccessTokenService
{
    public async Task<string> RefreshTokenAsync(string url, string refreshToken, string clientId)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", clientId)
        });

        request.Content = content;

        var response = await client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
}