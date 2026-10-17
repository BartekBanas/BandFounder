namespace BandFounder.Infrastructure.Spotify;

public enum SpotifyPlaylistReadTokenSource
{
    ClientCredentials,
    AppOwnerSignIn
}

public sealed class SpotifyPlaylistReadOptions
{
    public const string SectionName = "SpotifyPlaylistRead";

    public const string InvalidMarketMessage =
        "SpotifyPlaylistRead:Market must be a two-letter ISO 3166-1 alpha-2 country code.";

    public SpotifyPlaylistReadTokenSource TokenSource { get; set; } =
        SpotifyPlaylistReadTokenSource.ClientCredentials;

    public Guid AppOwnerAccountId { get; set; }

    public string Market { get; set; } = "US";

    public static bool IsValidMarket(string? market)
    {
        var trimmed = market?.Trim();
        return trimmed is { Length: 2 }
               && char.IsAsciiLetter(trimmed[0])
               && char.IsAsciiLetter(trimmed[1]);
    }

    public static string RequireMarket(string? market)
    {
        if (!IsValidMarket(market))
        {
            throw new InvalidOperationException(InvalidMarketMessage);
        }

        return market!.Trim().ToUpperInvariant();
    }
}
