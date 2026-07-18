namespace BandFounder.Infrastructure.Spotify;

public static class SpotifyTimeRange
{
    public const string ShortTerm = "short_term";
    public const string MediumTerm = "medium_term";
    public const string LongTerm = "long_term";
    public const string Default = MediumTerm;

    private static readonly HashSet<string> Allowed =
    [
        ShortTerm,
        MediumTerm,
        LongTerm
    ];

    public static string Normalize(string? timeRange)
    {
        if (string.IsNullOrWhiteSpace(timeRange)) {
            return Default;
        }

        var normalized = timeRange.Trim().ToLowerInvariant();
        return Allowed.Contains(normalized) ? normalized : Default;
    }

    public static int ClampLimit(int? limit, int defaultLimit = 50)
    {
        var value = limit ?? defaultLimit;
        return Math.Clamp(value, 1, 50);
    }
}