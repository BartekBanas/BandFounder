using System.Net.Http.Headers;

namespace BandFounder.Infrastructure.Spotify;

public static class SpotifyRetryAfter
{
    public static TimeSpan Parse(HttpResponseMessage response)
    {
        return Parse(response.Headers.RetryAfter);
    }

    public static TimeSpan Parse(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter?.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
        }

        return TimeSpan.FromSeconds(1);
    }
}
