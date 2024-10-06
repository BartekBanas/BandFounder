namespace BandFounder.Application.Services;

public class MusicTasteComparisonService
{
    private readonly IAccountService _accountService;
    private readonly IUserAuthenticationService _userAuthenticationService;

    public MusicTasteComparisonService(IAccountService accountService,
        IUserAuthenticationService userAuthenticationService)
    {
        _accountService = accountService;
        _userAuthenticationService = userAuthenticationService;
    }

    public async Task<int> CompareMusicTasteAsync(Guid userId)
    {
        var senderId = _userAuthenticationService.GetUserId();

        var genreSimilarityScore = await CalculateGenreSimilarity(senderId, userId);

        var artistSimilarityScore = await CalculateArtistSimilarity(senderId, userId);

        return genreSimilarityScore + artistSimilarityScore;
    }

    private async Task<int> CalculateGenreSimilarity(Guid userId1, Guid userId2)
    {
        var user1Genres = await GetWagedGenres(userId1);
        var user2Genres = await GetWagedGenres(userId2);

        var genreSimilarityScore = 0;

        var allGenres = user1Genres.Keys.Union(user2Genres.Keys);

        foreach (var genre in allGenres)
        {
            var user1Weight = user1Genres.GetValueOrDefault(genre, defaultValue: 0);
            var user2Weight = user2Genres.GetValueOrDefault(genre, defaultValue: 0);

            genreSimilarityScore += Math.Min(user1Weight, user2Weight);
        }

        return genreSimilarityScore;
    }

    private async Task<int> CalculateArtistSimilarity(Guid userId1, Guid userId2)
    {
        var user1 = await _accountService.GetDetailedAccount(userId1);
        var user2 = await _accountService.GetDetailedAccount(userId2);

        var user1ArtistIds = user1.Artists.Select(artist => artist.Id).ToHashSet();
        var user2ArtistIds = user2.Artists.Select(artist => artist.Id).ToHashSet();

        var commonArtists = user1ArtistIds.Intersect(user2ArtistIds).Count();

        var artistSimilarityScore = commonArtists * 3;

        return artistSimilarityScore;
    }

    private async Task<Dictionary<string, int>> GetWagedGenres(Guid userId)
    {
        var account = await _accountService.GetDetailedAccount(userId);

        var wagedGenres = new Dictionary<string, int>();

        foreach (var genre in account.Artists.SelectMany(artist => artist.Genres))
        {
            if (!wagedGenres.TryAdd(genre.Name, 1))
            {
                wagedGenres[genre.Name]++;
            }
        }

        var sortedWagedGenres = wagedGenres
            .OrderByDescending(genre => genre.Value)
            .ToDictionary(genre => genre.Key, genre => genre.Value);

        return sortedWagedGenres;
    }
}