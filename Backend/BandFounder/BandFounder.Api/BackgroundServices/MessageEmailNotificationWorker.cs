using BandFounder.Application.Services;
using BandFounder.Application.Services.Email;
using Microsoft.Extensions.Options;

namespace BandFounder.Api.BackgroundServices;

public sealed class MessageEmailNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MessageEmailNotificationOptions> options,
    ILogger<MessageEmailNotificationWorker> logger) : BackgroundService
{
    private readonly MessageEmailNotificationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

        try
        {
            do
            {
                await ProcessDueNotificationsAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task ProcessDueNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var processedCount = 0;
            bool processed;

            do
            {
                // One scope (and DbContext) per attempt so eligibility re-reads cannot
                // reuse Account / prefs / membership / read state tracked from earlier items.
                using var scope = scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider
                    .GetRequiredService<IMessageEmailNotificationService>();
                processed = await notificationService.ProcessDueAsync(cancellationToken);
                if (processed)
                {
                    processedCount++;
                }
            }
            while (processed);

            logger.LogInformation(
                "Processed {ProcessedCount} message email notifications in worker cycle",
                processedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Message email notification worker cycle failed");
        }
    }
}
