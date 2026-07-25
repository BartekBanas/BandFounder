namespace BandFounder.Application.Services.Spotify;

public static class TasteRefreshSchedule
{
    /// <summary>
    /// Returns true when the user's taste profile should be refreshed.
    /// Due at ArtistsSyncedAt + MinDays + (stableHash % span) days, so users spread across MinDays..MaxDays.
    /// Null ArtistsSyncedAt is always due.
    /// </summary>
    public static bool IsDue(
        Guid accountId,
        DateTime? artistsSyncedAt,
        DateTime utcNow,
        int minDays = 7,
        int maxDays = 14)
    {
        if (minDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minDays));
        }

        if (maxDays < minDays)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDays), "MaxDays must be >= MinDays.");
        }

        if (artistsSyncedAt is null)
        {
            return true;
        }

        var jitterDays = GetJitterDays(accountId, minDays, maxDays);
        var dueAt = artistsSyncedAt.Value.ToUniversalTime().AddDays(minDays + jitterDays);
        return utcNow.ToUniversalTime() >= dueAt;
    }

    public static bool IsDue(
        Guid accountId,
        DateTime? artistsSyncedAt,
        DateTime utcNow,
        TasteRefreshOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return IsDue(accountId, artistsSyncedAt, utcNow, options.MinDays, options.MaxDays);
    }

    /// <summary>Offset in [0, MaxDays - MinDays] inclusive, stable for a given accountId.</summary>
    public static int GetJitterDays(Guid accountId, int minDays = 7, int maxDays = 14)
    {
        if (maxDays < minDays)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDays));
        }

        var span = maxDays - minDays;
        if (span == 0)
        {
            return 0;
        }

        var hash = HashCode.Combine(accountId);
        // Make non-negative without Math.Abs(int.MinValue) overflow
        var positive = hash & int.MaxValue;
        return positive % (span + 1);
    }
}
