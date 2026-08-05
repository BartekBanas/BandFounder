using BandFounder.Application.Services;

namespace Api.IntegrationTests.Infrastructure;

public sealed class ControllableMessageEmailNotificationGate : IMessageEmailNotificationGate
{
    public TaskCompletionSource<bool>? EligibilityStarted { get; set; }
    public TaskCompletionSource<bool>? AllowEligibility { get; set; }

    public async Task WaitBeforeSendEligibilityAsync(CancellationToken cancellationToken = default)
    {
        EligibilityStarted?.TrySetResult(true);

        if (AllowEligibility is not null)
        {
            await AllowEligibility.Task.WaitAsync(cancellationToken);
        }
    }

    public void Clear()
    {
        EligibilityStarted = null;
        AllowEligibility = null;
    }
}
