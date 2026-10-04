using System.Text.RegularExpressions;

namespace BandFounder.Application.Services;

/// <summary>
/// Two tracks count as the same song when their names and artist sets match after lowercasing,
/// trimming, and collapsing whitespace. This catches the same song released on a single and an album.
/// </summary>
public static class SpotifyTrackDedupKey
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static string Create(string? trackName, IEnumerable<string?> artistNames)
    {
        var artists = artistNames
            .Select(Normalize)
            .Where(artist => artist.Length > 0)
            .Order(StringComparer.Ordinal);

        return $"{Normalize(trackName)}\u001f{string.Join('\u001e', artists)}";
    }

    public static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Whitespace.Replace(value.Trim().ToLowerInvariant(), " ");
    }
}
