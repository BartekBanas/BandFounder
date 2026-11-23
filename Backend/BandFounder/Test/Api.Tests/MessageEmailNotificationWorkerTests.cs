using BandFounder.Api.BackgroundServices;
using BandFounder.Application.Services;
using BandFounder.Application.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Api.Tests;

[TestFixture]
public class MessageEmailNotificationWorkerTests
{
    [Test]
    public async Task ProcessDueNotificationsAsync_ProcessesUntilNoMoreWork()
    {
        var service = Substitute.For<IMessageEmailNotificationService>();
        service.ProcessDueAsync(Arg.Any<CancellationToken>()).Returns(true, true, false);
        var worker = CreateWorker(service);

        await worker.ProcessDueNotificationsAsync(CancellationToken.None);

        await service.Received(3).ProcessDueAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessDueNotificationsAsync_NoWork_CallsServiceOnce()
    {
        var service = Substitute.For<IMessageEmailNotificationService>();
        service.ProcessDueAsync(Arg.Any<CancellationToken>()).Returns(false);
        var worker = CreateWorker(service);

        await worker.ProcessDueNotificationsAsync(CancellationToken.None);

        await service.Received(1).ProcessDueAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public void ProcessDueNotificationsAsync_ServiceThrows_DoesNotPropagate()
    {
        var service = Substitute.For<IMessageEmailNotificationService>();
        service.ProcessDueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("boom")));
        var worker = CreateWorker(service);

        Assert.DoesNotThrowAsync(() => worker.ProcessDueNotificationsAsync(CancellationToken.None));
    }

    [Test]
    public void ProcessDueNotificationsAsync_CancellationRequested_DoesNotPropagate()
    {
        var service = Substitute.For<IMessageEmailNotificationService>();
        service.ProcessDueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new OperationCanceledException()));
        var worker = CreateWorker(service);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.DoesNotThrowAsync(() => worker.ProcessDueNotificationsAsync(cts.Token));
    }

    private static MessageEmailNotificationWorker CreateWorker(IMessageEmailNotificationService service)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IMessageEmailNotificationService)).Returns(service);
        provider.GetService(typeof(IEmailVerificationService))
            .Returns(Substitute.For<IEmailVerificationService>());

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return new MessageEmailNotificationWorker(
            scopeFactory,
            Options.Create(new MessageEmailNotificationOptions()),
            Substitute.For<ILogger<MessageEmailNotificationWorker>>());
    }
}
