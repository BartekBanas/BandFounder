using System.Collections.Concurrent;
using BandFounder.Application.Services.Email;

namespace Api.IntegrationTests.Infrastructure;

public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentBag<OutgoingEmail> _sent = new();

    public bool ThrowOnSend { get; set; }

    public IReadOnlyList<OutgoingEmail> Sent => _sent.ToList();

    public Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        _sent.Add(email);

        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Simulated email send failure");
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _sent.Clear();
        ThrowOnSend = false;
    }
}
