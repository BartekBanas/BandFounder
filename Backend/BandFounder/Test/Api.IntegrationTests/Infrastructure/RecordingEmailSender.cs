using BandFounder.Application.Services.Email;

namespace Api.IntegrationTests.Infrastructure;

public sealed class RecordingEmailSender : IEmailSender
{
    private readonly object _lock = new();
    private readonly List<OutgoingEmail> _sent = [];

    public bool ThrowOnSend { get; set; }
    public bool ThrowAfterRecording { get; set; }
    public TaskCompletionSource<bool>? SendStarted { get; set; }
    public TaskCompletionSource<bool>? AllowSend { get; set; }

    public IReadOnlyList<OutgoingEmail> Sent
    {
        get
        {
            lock (_lock)
            {
                return _sent.ToList();
            }
        }
    }

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        SendStarted?.TrySetResult(true);

        if (AllowSend is not null)
        {
            await AllowSend.Task.WaitAsync(cancellationToken);
        }

        if (ThrowOnSend && !ThrowAfterRecording)
        {
            throw new InvalidOperationException("Simulated email send failure");
        }

        lock (_lock)
        {
            _sent.Add(email);
        }

        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Simulated ambiguous email send failure");
        }
    }

    public void RemoveSent(IEnumerable<OutgoingEmail> emails)
    {
        var removed = emails.ToHashSet(ReferenceEqualityComparer.Instance);
        lock (_lock)
        {
            _sent.RemoveAll(removed.Contains);
        }
    }

    public void ClearSent()
    {
        lock (_lock)
        {
            _sent.Clear();
        }
    }

    public void Reset()
    {
        ClearSent();
        ThrowOnSend = false;
        ThrowAfterRecording = false;
        SendStarted = null;
        AllowSend = null;
    }
}
