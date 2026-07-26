using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace BandFounder.Application.Services;

public sealed record MusicProfile(
    IReadOnlySet<string> ArtistIds,
    IReadOnlyDictionary<string, int> GenreWeights);

public interface IMusicProfileProvider
{
    Task<IReadOnlyDictionary<Guid, MusicProfile>> GetProfilesAsync(IReadOnlyCollection<Guid> accountIds);
    void Invalidate(Guid accountId);
}

public sealed class MusicProfileProvider(
    IRepository<Account> accountRepository,
    IMemoryCache cache) : IMusicProfileProvider
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyDictionary<Guid, MusicProfile>> GetProfilesAsync(
        IReadOnlyCollection<Guid> accountIds)
    {
        var requestedAccountIds = accountIds.Distinct().ToArray();
        var profiles = new Dictionary<Guid, MusicProfile>(requestedAccountIds.Length);
        var missingAccountIds = new List<Guid>();

        foreach (var accountId in requestedAccountIds)
        {
            if (cache.TryGetValue(GetCacheKey(accountId), out MusicProfile? profile) && profile is not null)
            {
                profiles[accountId] = profile;
            }
            else
            {
                missingAccountIds.Add(accountId);
            }
        }

        if (missingAccountIds.Count == 0)
        {
            return profiles;
        }

        var artistRows = await accountRepository.QueryAsync(accounts =>
            accounts
                .Where(account => missingAccountIds.Contains(account.Id))
                .SelectMany(account => account.Artists.Select(artist =>
                    new ArtistProfileRow(account.Id, artist.Id))));

        var genreRows = await accountRepository.QueryAsync(accounts =>
            accounts
                .Where(account => missingAccountIds.Contains(account.Id))
                .SelectMany(account => account.Artists.SelectMany(artist => artist.Genres)
                    .Select(genre => new { AccountId = account.Id, GenreName = genre.Name }))
                .GroupBy(row => new { row.AccountId, row.GenreName })
                .Select(group => new GenreWeightProfileRow(
                    group.Key.AccountId,
                    group.Key.GenreName,
                    group.Count())));

        foreach (var accountId in missingAccountIds)
        {
            var profile = new MusicProfile(
                artistRows
                    .Where(row => row.AccountId == accountId)
                    .Select(row => row.ArtistId)
                    .ToHashSet(),
                genreRows
                    .Where(row => row.AccountId == accountId)
                    .ToDictionary(row => row.GenreName, row => row.Weight));

            cache.Set(GetCacheKey(accountId), profile, CacheLifetime);
            profiles[accountId] = profile;
        }

        return profiles;
    }

    public void Invalidate(Guid accountId)
    {
        cache.Remove(GetCacheKey(accountId));
    }

    private static string GetCacheKey(Guid accountId) => $"music-profile:{accountId}";

    private sealed record ArtistProfileRow(Guid AccountId, string ArtistId);
    private sealed record GenreProfileRow(Guid AccountId, string GenreName);
    private sealed record GenreWeightProfileRow(Guid AccountId, string GenreName, int Weight);
}
