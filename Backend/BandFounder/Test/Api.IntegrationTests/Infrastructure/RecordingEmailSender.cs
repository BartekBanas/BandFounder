using System.Collections.Concurrent;
using BandFounder.Application.Services.Email;

namespace Api.IntegrationTests.Infrastructure;

public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentBag<OutgoingEmail> _sent = new();

    public bool ThrowOnSend { get; set; }
    public TaskCompletionSource<bool>? SendStarted { get; set; }
    public TaskCompletionSource<bool>? AllowSend { get; set; }

    public IReadOnlyList<OutgoingEmail> Sent => _sent.ToList();

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        SendStarted?.TrySetResult(true);

        if (AllowSend is not null)
        {
            await AllowSend.Task.WaitAsync(cancellationToken);
        }

        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Simulated email send failure");
        }

        _sent.Add(email);
    }

    public void Clear()
    {
        _sent.Clear();
        ThrowOnSend = false;
        SendStarted = null;
        AllowSend = null;
    }
}
