using System.Collections.Concurrent;

namespace BandFounder.Application.Services;

public interface IMusicProfileVersionRegistry
{
    long GetVersion(Guid accountId);
    void Bump(Guid accountId);
    void BumpAndRun(Guid accountId, Action action);
    bool TryRunIfCurrent(Guid accountId, long version, Action action);
}

/// <summary>
/// Must be registered as a singleton: invalidations and profile loads happen in different request scopes,
/// so the version counters have to be shared process-wide to be observed across them.
/// </summary>
public sealed class MusicProfileVersionRegistry : IMusicProfileVersionRegistry
{
    private readonly ConcurrentDictionary<Guid, long> _versions = new();
    private readonly ConcurrentDictionary<Guid, object> _locks = new();

    public long GetVersion(Guid accountId)
    {
        lock (GetLock(accountId))
        {
            return _versions.GetValueOrDefault(accountId);
        }
    }

    public void Bump(Guid accountId)
    {
        lock (GetLock(accountId))
        {
            _versions.AddOrUpdate(accountId, 1, static (_, version) => version + 1);
        }
    }

    public void BumpAndRun(Guid accountId, Action action)
    {
        lock (GetLock(accountId))
        {
            _versions.AddOrUpdate(accountId, 1, static (_, version) => version + 1);
            action();
        }
    }

    public bool TryRunIfCurrent(Guid accountId, long version, Action action)
    {
        lock (GetLock(accountId))
        {
            if (_versions.GetValueOrDefault(accountId) != version)
            {
                return false;
            }

            action();
            return true;
        }
    }

    private object GetLock(Guid accountId) => _locks.GetOrAdd(accountId, static _ => new object());
}
