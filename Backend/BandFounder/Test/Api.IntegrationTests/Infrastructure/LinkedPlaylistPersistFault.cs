using BandFounder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.IntegrationTests.Infrastructure;

public sealed class LinkedPlaylistPersistFault
{
    private int _remaining;

    public void FailNextLinkedPlaylistSave() => Interlocked.Exchange(ref _remaining, 1);

    public void Clear() => Interlocked.Exchange(ref _remaining, 0);

    private bool TryConsume() => Interlocked.Exchange(ref _remaining, 0) > 0;

    public void ThrowIfLinkedPlaylistSaveShouldFail(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var addingLinkedPlaylist = context.ChangeTracker
            .Entries<LinkedPlaylist>()
            .Any(entry => entry.State == EntityState.Added);

        if (!addingLinkedPlaylist || !TryConsume())
        {
            return;
        }

        throw new InvalidOperationException("Forced linked-playlist persist failure.");
    }
}

public sealed class LinkedPlaylistPersistFaultInterceptor : SaveChangesInterceptor
{
    private readonly LinkedPlaylistPersistFault _fault;

    public LinkedPlaylistPersistFaultInterceptor(LinkedPlaylistPersistFault fault)
    {
        _fault = fault;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        _fault.ThrowIfLinkedPlaylistSaveShouldFail(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _fault.ThrowIfLinkedPlaylistSaveShouldFail(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
