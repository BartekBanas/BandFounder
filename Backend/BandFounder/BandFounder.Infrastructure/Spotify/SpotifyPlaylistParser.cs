using System.Diagnostics.CodeAnalysis;

namespace BandFounder.Infrastructure.Spotify;

public abstract record SpotifyPlaylistParseResult
{
    public sealed record PlaylistId(SpotifyPlaylistId Id) : SpotifyPlaylistParseResult;

    public sealed record ShortLink(Uri Url) : SpotifyPlaylistParseResult;

    public sealed record Invalid : SpotifyPlaylistParseResult;
}

public static class SpotifyPlaylistParser
{
    public static SpotifyPlaylistParseResult Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new SpotifyPlaylistParseResult.Invalid();
        }

        var trimmed = input.Trim();
        if (SpotifyShortLink.TryCreate(trimmed, out var shortLink))
        {
            return new SpotifyPlaylistParseResult.ShortLink(shortLink);
        }

        if (SpotifyPlaylistId.TryParse(trimmed, out var playlistId))
        {
            return new SpotifyPlaylistParseResult.PlaylistId(playlistId);
        }

        return new SpotifyPlaylistParseResult.Invalid();
    }
}

public static class SpotifyShortLink
{
    public static bool TryCreate(string trimmed, [NotNullWhen(true)] out Uri? shortLink)
    {
        var candidate = trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var url) &&
            SpotifyPlaylistId.IsHttpOrHttps(url) &&
            SpotifyPlaylistId.HostnameOf(url) == "spotify.link")
        {
            shortLink = url;
            return true;
        }

        shortLink = null;
        return false;
    }
}
