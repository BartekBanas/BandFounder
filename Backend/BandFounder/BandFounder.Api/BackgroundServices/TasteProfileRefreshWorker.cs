using BandFounder.Application.Services;
using BandFounder.Application.Services.Spotify;
using BandFounder.Infrastructure.Spotify.Exceptions;
using Microsoft.Extensions.Options;

namespace BandFounder.Api.BackgroundServices;

public class TasteProfileRefreshWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TasteRefreshOptions> options,
    ILogger<TasteProfileRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Taste profile refresh worker is disabled.");
            return;
        }

        var tick = TimeSpan.FromHours(Math.Max(1, settings.TickHours));
        using var timer = new PeriodicTimer(tick);

        // First pass after a short delay so the app can finish starting
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        do
        {
            try
            {
                await RefreshDueUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Taste profile refresh tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshDueUsersAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        await using var scope = scopeFactory.CreateAsyncScope();
        var spotifyConnectionService = scope.ServiceProvider.GetRequiredService<ISpotifyConnectionService>();

        var dueAccountIds = await spotifyConnectionService.GetAccountIdsDueForTasteRefreshAsync(settings.BatchSize);
        if (dueAccountIds.Count == 0)
        {
            return;
        }

        logger.LogInformation("Refreshing taste profiles for {Count} accounts.", dueAccountIds.Count);

        foreach (var accountId in dueAccountIds)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                await spotifyConnectionService.SaveRelevantArtists(accountId);
            }
            catch (SpotifyReauthorizationRequiredException)
            {
                logger.LogWarning(
                    "Skipped taste refresh for {AccountId}: Spotify reauthorization required.",
                    accountId);
            }
            catch (SpotifyAccountNotLinkedException)
            {
                logger.LogWarning(
                    "Skipped taste refresh for {AccountId}: Spotify account not linked.",
                    accountId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Taste refresh failed for account {AccountId}.", accountId);
            }
        }
    }
}
