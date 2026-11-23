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
                var messageNotificationService = scope.ServiceProvider
                    .GetRequiredService<IMessageEmailNotificationService>();
                var emailVerificationService = scope.ServiceProvider
                    .GetRequiredService<IEmailVerificationService>();
                var processedVerification =
                    await emailVerificationService.ProcessDueAsync(cancellationToken);
                var processedMessage =
                    await messageNotificationService.ProcessDueAsync(cancellationToken);
                processed = processedVerification || processedMessage;
                if (processed)
                {
                    processedCount++;
                }
            }
            while (processed);

            logger.LogInformation(
                "Processed {ProcessedCount} email delivery batches in worker cycle",
                processedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Email delivery worker cycle failed");
        }
    }
}
