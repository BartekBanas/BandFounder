using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services;

public interface IMusicTasteComparisonService
{
    Task<IEnumerable<string>> GetCommonArtists(Guid requesterId, Guid targetUserId);
    Task<IEnumerable<string>> GetCommonGenres(Guid requesterId, Guid targetUserId);
    Task<int> CompareMusicTasteAsync(Guid requesterId, Guid targetUserId);
}

public class MusicTasteComparisonService : IMusicTasteComparisonService
{
    private readonly IAccountService _accountService;

    public MusicTasteComparisonService(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<IEnumerable<string>> GetCommonArtists(Guid requesterId, Guid targetUserId)
    {
        var user1 = await _accountService.GetDetailedAccount(requesterId);
        var user2 = await _accountService.GetDetailedAccount(targetUserId);
        
        var user1ArtistIds = user1.Artists.Select(artist => artist.Id).ToHashSet();
        var user2ArtistIds = user2.Artists.Select(artist => artist.Id).ToHashSet();

        var commonArtists = user1ArtistIds.Intersect(user2ArtistIds);

        return commonArtists;
    }

    public async Task<IEnumerable<string>> GetCommonGenres(Guid requesterId, Guid targetUserId)
    {
        var user1 = await _accountService.GetDetailedAccount(requesterId);
        var user2 = await _accountService.GetDetailedAccount(targetUserId);
        
        var user1Genres = GetWagedGenres(user1);
        var user2Genres = GetWagedGenres(user2);

        var commonGenres = user1Genres.Keys
            .Intersect(user2Genres.Keys)
            .OrderByDescending(genre => Math.Min(user1Genres[genre], user2Genres[genre]));
        
        return commonGenres;
    }

    public async Task<int> CompareMusicTasteAsync(Guid requesterId, Guid targetUserId)
    {
        var user1 = await _accountService.GetDetailedAccount(requesterId);
        var user2 = await _accountService.GetDetailedAccount(targetUserId);

        var genreSimilarityScore = CalculateGenreSimilarity(user1, user2);
        var artistSimilarityScore = CalculateArtistSimilarity(user1, user2);

        return genreSimilarityScore + artistSimilarityScore;
    }

    private int CalculateGenreSimilarity(Account user1, Account user2)
    {
        var user1Genres = GetWagedGenres(user1);
        var user2Genres = GetWagedGenres(user2);

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

    private int CalculateArtistSimilarity(Account user1, Account user2)
    {
        var user1ArtistIds = user1.Artists.Select(artist => artist.Id).ToHashSet();
        var user2ArtistIds = user2.Artists.Select(artist => artist.Id).ToHashSet();

        var commonArtists = user1ArtistIds.Intersect(user2ArtistIds).Count();

        var artistSimilarityScore = commonArtists * 3;

        return artistSimilarityScore;
    }

    private Dictionary<string, int> GetWagedGenres(Account account)
    {
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