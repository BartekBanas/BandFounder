using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace BandFounder.Application.Services;

public sealed record MusicProfile(
    IReadOnlySet<string> ArtistIds,
    IReadOnlyDictionary<string, int> GenreWeights);

public sealed record MusicProfileArtistRow(Guid AccountId, string ArtistId);
public sealed record MusicProfileGenreWeightRow(Guid AccountId, string GenreName, int Weight);

public interface IMusicProfileProvider
{
    Task<IReadOnlyDictionary<Guid, MusicProfile>> GetProfilesAsync(IReadOnlyCollection<Guid> accountIds);
    void Invalidate(Guid accountId);
    Task InvalidateForArtistsAsync(IReadOnlyCollection<string> artistIds);
}

public sealed class MusicProfileProvider(
    IRepository<Account> accountRepository,
    IMemoryCache cache,
    IMusicProfileVersionRegistry versions) : IMusicProfileProvider
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

        var versionsAtReadStart = missingAccountIds.ToDictionary(
            accountId => accountId,
            versions.GetVersion);

        var artistRows = await accountRepository.QueryAsync(accounts =>
            accounts
                .Where(account => missingAccountIds.Contains(account.Id))
                .SelectMany(account => account.Artists.Select(artist =>
                    new MusicProfileArtistRow(account.Id, artist.Id))));

        var genreRows = await accountRepository.QueryAsync(accounts =>
            accounts
                .Where(account => missingAccountIds.Contains(account.Id))
                .SelectMany(account => account.Artists.SelectMany(artist => artist.Genres)
                    .Select(genre => new { AccountId = account.Id, GenreName = genre.Name }))
                .GroupBy(row => new { row.AccountId, row.GenreName })
                .Select(group => new MusicProfileGenreWeightRow(
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

            // Skip caching if Invalidate ran while we were loading; still return the snapshot for this request.
            versions.TryRunIfCurrent(
                accountId,
                versionsAtReadStart[accountId],
                () => cache.Set(GetCacheKey(accountId), profile, CacheLifetime));

            profiles[accountId] = profile;
        }

        return profiles;
    }

    public void Invalidate(Guid accountId)
    {
        versions.BumpAndRun(accountId, () => cache.Remove(GetCacheKey(accountId)));
    }

    public async Task InvalidateForArtistsAsync(IReadOnlyCollection<string> artistIds)
    {
        var distinctArtistIds = artistIds.Distinct().ToArray();
        if (distinctArtistIds.Length == 0)
        {
            return;
        }

        var accountIds = await accountRepository.QueryAsync(accounts =>
            accounts
                .Where(account => account.Artists.Any(artist => distinctArtistIds.Contains(artist.Id)))
                .Select(account => account.Id));

        foreach (var accountId in accountIds)
        {
            Invalidate(accountId);
        }
    }

    private static string GetCacheKey(Guid accountId) => $"music-profile:{accountId}";
}
