using BandFounder.Application.Dtos.Listings;
using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services;

public interface IMusicTasteService
{
    Task<IEnumerable<string>> GetCommonArtists(Guid requesterId, Guid targetUserId);
    Task<IEnumerable<string>> GetCommonGenres(Guid requesterId, Guid targetUserId);
    Task<ArtistsAndGenresDto> GetCommonArtistsAndGenresAsync(Guid targetUserId, Guid? accountId = null);
    Task<int> CompareMusicTasteAsync(Guid requesterId, Guid targetUserId);
    Task<IReadOnlyDictionary<Guid, int>> CompareMusicTasteManyAsync(
        Guid requesterId,
        IReadOnlyCollection<Guid> targetUserIds);
}

public class MusicTasteService : IMusicTasteService
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IMusicProfileProvider _musicProfileProvider;

    public MusicTasteService(
        IAccountService accountService,
        IAuthenticationService authenticationService,
        IMusicProfileProvider musicProfileProvider)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _musicProfileProvider = musicProfileProvider;
    }

    public async Task<ArtistsAndGenresDto> GetCommonArtistsAndGenresAsync(Guid targetUserId, Guid? accountId = null)
    {
        var requesterId = accountId ?? _authenticationService.GetUserId();

        var commonArtists = await GetCommonArtists(requesterId, targetUserId);
        var commonGenres = await GetCommonGenres(requesterId, targetUserId);

        return new ArtistsAndGenresDto(commonArtists, commonGenres);
    }

    public async Task<IEnumerable<string>> GetCommonArtists(Guid requesterId, Guid targetUserId)
    {
        var user1 = await _accountService.GetDetailedAccount(requesterId);
        var user2 = await _accountService.GetDetailedAccount(targetUserId);
        
        var user1ArtistIds = user1.Artists.Select(artist => artist.Name).ToHashSet();
        var user2ArtistIds = user2.Artists.Select(artist => artist.Name).ToHashSet();

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

    public Dictionary<string, int> GetWagedGenres(Account account)
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

    public async Task<int> CompareMusicTasteAsync(Guid requesterId, Guid targetUserId)
    {
        var scores = await CompareMusicTasteManyAsync(requesterId, [targetUserId]);
        return scores[targetUserId];
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CompareMusicTasteManyAsync(
        Guid requesterId,
        IReadOnlyCollection<Guid> targetUserIds)
    {
        var distinctTargetUserIds = targetUserIds.Distinct().ToArray();
        var profiles = await _musicProfileProvider.GetProfilesAsync(
            [requesterId, .. distinctTargetUserIds]);
        var requesterProfile = profiles[requesterId];

        return distinctTargetUserIds.ToDictionary(
            targetUserId => targetUserId,
            targetUserId => MusicSimilarity.Score(requesterProfile, profiles[targetUserId]));
    }
    
}