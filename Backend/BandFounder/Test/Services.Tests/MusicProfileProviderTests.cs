using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class MusicProfileProviderTests
{
    [Test]
    public async Task GetProfilesAsync_ShouldCacheProfilesUntilInvalidated()
    {
        var accountId = Guid.NewGuid();
        var accountRepository = Substitute.For<IRepository<Account>>();
        accountRepository
            .QueryAsync<MusicProfileArtistRow>(
                Arg.Any<Func<IQueryable<Account>, IQueryable<MusicProfileArtistRow>>>())
            .Returns(
            [
                new MusicProfileArtistRow(accountId, "artist-1")
            ]);
        accountRepository
            .QueryAsync<MusicProfileGenreWeightRow>(
                Arg.Any<Func<IQueryable<Account>, IQueryable<MusicProfileGenreWeightRow>>>())
            .Returns(
            [
                new MusicProfileGenreWeightRow(accountId, "Rock", 2)
            ]);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new MusicProfileProvider(accountRepository, cache);

        var first = await provider.GetProfilesAsync([accountId]);
        var cached = await provider.GetProfilesAsync([accountId]);

        Assert.That(first[accountId].ArtistIds, Is.EquivalentTo(new[] { "artist-1" }));
        Assert.That(first[accountId].GenreWeights["Rock"], Is.EqualTo(2));
        Assert.That(cached, Is.EqualTo(first));
        await accountRepository.Received(1)
            .QueryAsync<MusicProfileArtistRow>(
                Arg.Any<Func<IQueryable<Account>, IQueryable<MusicProfileArtistRow>>>());

        provider.Invalidate(accountId);
        await provider.GetProfilesAsync([accountId]);

        await accountRepository.Received(2)
            .QueryAsync<MusicProfileArtistRow>(
                Arg.Any<Func<IQueryable<Account>, IQueryable<MusicProfileArtistRow>>>());
    }

    [Test]
    public async Task InvalidateForArtistsAsync_ShouldInvalidateEveryLinkedAccount()
    {
        var firstAccountId = Guid.NewGuid();
        var secondAccountId = Guid.NewGuid();
        var accountRepository = Substitute.For<IRepository<Account>>();
        accountRepository
            .QueryAsync<Guid>(Arg.Any<Func<IQueryable<Account>, IQueryable<Guid>>>())
            .Returns([firstAccountId, secondAccountId]);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(
            $"music-profile:{firstAccountId}",
            new MusicProfile(new HashSet<string>(), new Dictionary<string, int>()));
        cache.Set(
            $"music-profile:{secondAccountId}",
            new MusicProfile(new HashSet<string>(), new Dictionary<string, int>()));
        var provider = new MusicProfileProvider(accountRepository, cache);

        await provider.InvalidateForArtistsAsync(["artist-1"]);

        Assert.That(cache.TryGetValue($"music-profile:{firstAccountId}", out _), Is.False);
        Assert.That(cache.TryGetValue($"music-profile:{secondAccountId}", out _), Is.False);
    }

    [Test]
    public async Task GetProfilesAsync_ShouldNotCacheWhenInvalidatedDuringLoad()
    {
        var accountId = Guid.NewGuid();
        var accountRepository = Substitute.For<IRepository<Account>>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new MusicProfileProvider(accountRepository, cache);
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowLoadToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        accountRepository
            .QueryAsync<MusicProfileArtistRow>(
                Arg.Any<Func<IQueryable<Account>, IQueryable<MusicProfileArtistRow>>>())
            .Returns(async _ =>
            {
                loadStarted.TrySetResult();
                await allowLoadToFinish.Task;
                return
                [
                    new MusicProfileArtistRow(accountId, "artist-1")
                ];
            });
        accountRepository
            .QueryAsync<MusicProfileGenreWeightRow>(
                Arg.Any<Func<IQueryable<Account>, IQueryable<MusicProfileGenreWeightRow>>>())
            .Returns(
            [
                new MusicProfileGenreWeightRow(accountId, "Rock", 1)
            ]);

        var loadTask = provider.GetProfilesAsync([accountId]);
        await loadStarted.Task;
        provider.Invalidate(accountId);
        allowLoadToFinish.TrySetResult();

        var profiles = await loadTask;

        Assert.That(profiles[accountId].ArtistIds, Is.EquivalentTo(new[] { "artist-1" }));
        Assert.That(cache.TryGetValue($"music-profile:{accountId}", out _), Is.False);
    }
}
