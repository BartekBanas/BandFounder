using BandFounder.Application.Services;
using BandFounder.Application.Services.Email;
using Microsoft.Extensions.Options;

namespace BandFounder.Api.BackgroundServices;

public sealed class EmailVerificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MessageEmailNotificationOptions> options,
    ILogger<EmailVerificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));
        do
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using var scope = scopeFactory.CreateScope();
                    var verification = scope.ServiceProvider.GetRequiredService<IEmailVerificationService>();
                    if (!await verification.ProcessDueAsync(stoppingToken))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Email verification delivery cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
