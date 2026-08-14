namespace BandFounder.Application.Services;

public static class SpotifyGenreMatcher
{
    public static bool ArtistMatchesGenre(IEnumerable<string> artistGenres, string genreQuery)
    {
        if (string.IsNullOrWhiteSpace(genreQuery))
        {
            return false;
        }

        var normalizedQuery = genreQuery.Trim();
        return artistGenres.Any(genre =>
            !string.IsNullOrWhiteSpace(genre)
            && genre.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
    }

    public static bool TrackMatchesGenre(
        IEnumerable<string?> artistIds,
        IReadOnlyDictionary<string, IReadOnlyList<string>> genresByArtistId,
        string genreQuery)
    {
        foreach (var artistId in artistIds)
        {
            if (string.IsNullOrWhiteSpace(artistId))
            {
                continue;
            }

            if (!genresByArtistId.TryGetValue(artistId, out var genres))
            {
                continue;
            }

            if (ArtistMatchesGenre(genres, genreQuery))
            {
                return true;
            }
        }

        return false;
    }
}
