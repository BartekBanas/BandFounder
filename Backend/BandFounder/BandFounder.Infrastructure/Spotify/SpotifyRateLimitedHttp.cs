using System.Net;

namespace BandFounder.Infrastructure.Spotify;

public static class SpotifyRateLimitedHttp
{
    public const int MaxRetries = 3;

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> createRequest)
    {
        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            response?.Dispose();
            using var request = createRequest();
            response = await httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt == MaxRetries)
            {
                return response;
            }

            await Task.Delay(SpotifyRetryAfter.Parse(response));
        }

        return response!;
    }
}
