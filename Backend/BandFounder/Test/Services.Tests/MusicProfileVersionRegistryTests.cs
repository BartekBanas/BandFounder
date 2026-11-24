using BandFounder.Application.Services;

namespace Services.Tests;

[TestFixture]
public class MusicProfileVersionRegistryTests
{
    [Test]
    public void GetVersion_ReturnsZero_ForUnknownAccount()
    {
        var registry = new MusicProfileVersionRegistry();

        Assert.That(registry.GetVersion(Guid.NewGuid()), Is.EqualTo(0));
    }

    [Test]
    public void Bump_IsIsolatedPerAccount()
    {
        var registry = new MusicProfileVersionRegistry();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        registry.Bump(accountA);
        registry.Bump(accountA);
        registry.Bump(accountB);

        Assert.That(registry.GetVersion(accountA), Is.EqualTo(2));
        Assert.That(registry.GetVersion(accountB), Is.EqualTo(1));
    }

    [Test]
    public void BumpAndRun_IncrementsVersionAndRunsAction()
    {
        var registry = new MusicProfileVersionRegistry();
        var accountId = Guid.NewGuid();
        var ran = false;

        registry.BumpAndRun(accountId, () => ran = true);

        Assert.That(ran, Is.True);
        Assert.That(registry.GetVersion(accountId), Is.EqualTo(1));
    }

    [Test]
    public void TryRunIfCurrent_RunsActionAndReturnsTrue_WhenVersionMatches()
    {
        var registry = new MusicProfileVersionRegistry();
        var accountId = Guid.NewGuid();
        var version = registry.GetVersion(accountId);
        var ran = false;

        var result = registry.TryRunIfCurrent(accountId, version, () => ran = true);

        Assert.That(result, Is.True);
        Assert.That(ran, Is.True);
    }

    [Test]
    public void TryRunIfCurrent_DoesNotRunActionAndReturnsFalse_WhenVersionStale()
    {
        var registry = new MusicProfileVersionRegistry();
        var accountId = Guid.NewGuid();
        var version = registry.GetVersion(accountId);
        registry.Bump(accountId);
        var ran = false;

        var result = registry.TryRunIfCurrent(accountId, version, () => ran = true);

        Assert.That(result, Is.False);
        Assert.That(ran, Is.False);
    }

    [Test]
    public void TryRunIfCurrent_RunsAction_WhenVersionIsCurrentAgain()
    {
        var registry = new MusicProfileVersionRegistry();
        var accountId = Guid.NewGuid();
        registry.Bump(accountId);
        var current = registry.GetVersion(accountId);
        var ran = false;

        var result = registry.TryRunIfCurrent(accountId, current, () => ran = true);

        Assert.That(result, Is.True);
        Assert.That(ran, Is.True);
    }

    [Test]
    public void Bump_IsThreadSafe()
    {
        var registry = new MusicProfileVersionRegistry();
        var accountId = Guid.NewGuid();
        const int iterations = 1000;

        Parallel.For(0, iterations, _ => registry.Bump(accountId));

        Assert.That(registry.GetVersion(accountId), Is.EqualTo(iterations));
    }
}
