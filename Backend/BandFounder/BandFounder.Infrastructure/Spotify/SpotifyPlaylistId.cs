using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace BandFounder.Infrastructure.Spotify;

/// <summary>
/// Accepts a playlist URL (any open.spotify.com variant), a spotify:playlist: URI, or a bare 22-character id.
/// </summary>
public static class SpotifyPlaylistId
{
    private static readonly Regex BareId = new("^[A-Za-z0-9]{22}$", RegexOptions.Compiled);
    private static readonly Regex EmbeddedId = new(
        "playlist[/:]([A-Za-z0-9]{22})(?![A-Za-z0-9])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParse(string? input, [NotNullWhen(true)] out string? playlistId)
    {
        playlistId = null;
        var trimmed = input?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        if (BareId.IsMatch(trimmed))
        {
            playlistId = trimmed;
            return true;
        }

        var match = EmbeddedId.Match(trimmed);
        if (!match.Success)
        {
            return false;
        }

        playlistId = match.Groups[1].Value;
        return true;
    }
}
