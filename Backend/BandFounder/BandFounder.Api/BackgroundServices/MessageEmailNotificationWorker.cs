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

    private async Task ProcessDueNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<IMessageEmailNotificationService>();
            var processedCount = 0;

            while (await notificationService.ProcessDueAsync(cancellationToken))
            {
                processedCount++;
            }

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
