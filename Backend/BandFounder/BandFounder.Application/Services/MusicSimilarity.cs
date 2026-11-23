namespace BandFounder.Application.Services;

public static class MusicSimilarity
{
    public static int Score(MusicProfile first, MusicProfile second)
    {
        var genreSimilarity = first.GenreWeights.Sum(genre =>
            Math.Min(genre.Value, second.GenreWeights.GetValueOrDefault(genre.Key)));

        var artistSimilarity = first.ArtistIds.Intersect(second.ArtistIds).Count() * 3;

        return genreSimilarity + artistSimilarity;
    }
}
