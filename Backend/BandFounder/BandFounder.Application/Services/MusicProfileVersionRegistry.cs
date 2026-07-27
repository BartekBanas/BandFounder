using System.Collections.Concurrent;

namespace BandFounder.Application.Services;

public interface IMusicProfileVersionRegistry
{
    long GetVersion(Guid accountId);
    void Bump(Guid accountId);
}

/// <summary>
/// Must be registered as a singleton: invalidations and profile loads happen in different request scopes,
/// so the version counters have to be shared process-wide to be observed across them.
/// </summary>
public sealed class MusicProfileVersionRegistry : IMusicProfileVersionRegistry
{
    private readonly ConcurrentDictionary<Guid, long> _versions = new();

    public long GetVersion(Guid accountId) => _versions.GetValueOrDefault(accountId);

    public void Bump(Guid accountId) =>
        _versions.AddOrUpdate(accountId, 1, static (_, version) => version + 1);
}
