using System.Text.RegularExpressions;

namespace BandFounder.Infrastructure.Spotify;

public readonly partial record struct SpotifyPlaylistId
{
    public const int Length = 22;

    public string Value { get; }

    private SpotifyPlaylistId(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public static bool TryParse(string? input, out SpotifyPlaylistId playlistId)
    {
        playlistId = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (TryReadId(trimmed, out var value))
        {
            playlistId = new SpotifyPlaylistId(value);
            return true;
        }

        return false;
    }

    private static bool TryReadId(string trimmed, out string value)
    {
        if (PlaylistIdRegex().IsMatch(trimmed))
        {
            value = trimmed;
            return true;
        }

        const string uriPrefix = "spotify:playlist:";
        if (trimmed.StartsWith(uriPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = trimmed[uriPrefix.Length..];
            var id = remainder.Split(['/', '?', '#'], 2)[0];
            if (PlaylistIdRegex().IsMatch(id))
            {
                value = id;
                return true;
            }

            value = string.Empty;
            return false;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var url) ||
            !IsHttpOrHttps(url) ||
            HostnameOf(url) != "open.spotify.com")
        {
            value = string.Empty;
            return false;
        }

        var match = PlaylistPathRegex().Match(url.AbsolutePath);
        if (match.Success)
        {
            value = match.Groups[1].Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    internal static bool IsHttpOrHttps(Uri url) =>
        url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps;

    internal static string HostnameOf(Uri url)
    {
        var host = url.Host.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal)
            ? host["www.".Length..]
            : host;
    }

    [GeneratedRegex("^[0-9A-Za-z]{22}$", RegexOptions.CultureInvariant)]
    private static partial Regex PlaylistIdRegex();

    [GeneratedRegex("/playlist/([0-9A-Za-z]{22})(?:/|$)", RegexOptions.CultureInvariant)]
    private static partial Regex PlaylistPathRegex();
}
