using BandFounder.Application.Services;
using BandFounder.Domain.Entities;

namespace Api.IntegrationTests.Infrastructure;

public sealed class QueueFailureGate
{
    public bool ThrowOnQueue { get; set; }

    public void Clear() => ThrowOnQueue = false;
}

public sealed class ControllableMessageEmailNotificationService : IMessageEmailNotificationService
{
    private readonly MessageEmailNotificationService _inner;
    private readonly QueueFailureGate _gate;

    public ControllableMessageEmailNotificationService(
        MessageEmailNotificationService inner,
        QueueFailureGate gate)
    {
        _inner = inner;
        _gate = gate;
    }

    public Task QueueAsync(Chatroom chatroom, Message message, Guid senderId)
    {
        if (_gate.ThrowOnQueue)
        {
            throw new InvalidOperationException("Simulated email notification queue failure");
        }

        return _inner.QueueAsync(chatroom, message, senderId);
    }

    public Task<bool> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        return _inner.ProcessDueAsync(cancellationToken);
    }
}
