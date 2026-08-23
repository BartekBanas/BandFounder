namespace BandFounder.Application.Services;

/// <summary>
/// Optional seam so tests can pause after claim and before eligibility is re-checked from the database.
/// </summary>
public interface IMessageEmailNotificationGate
{
    Task WaitBeforeSendEligibilityAsync(CancellationToken cancellationToken = default);
}

public sealed class NoOpMessageEmailNotificationGate : IMessageEmailNotificationGate
{
    public Task WaitBeforeSendEligibilityAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
