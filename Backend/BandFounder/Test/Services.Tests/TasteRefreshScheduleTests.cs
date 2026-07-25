using BandFounder.Application.Services.Spotify;

namespace Services.Tests;

[TestFixture]
public class TasteRefreshScheduleTests
{
    [Test]
    public void IsDue_ReturnsTrue_WhenNeverSynced()
    {
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Assert.That(
            TasteRefreshSchedule.IsDue(accountId, artistsSyncedAt: null, DateTime.UtcNow),
            Is.True);
    }

    [Test]
    public void IsDue_ReturnsFalse_WhenWithinMinDays()
    {
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var syncedAt = DateTime.UtcNow.AddDays(-3);

        Assert.That(
            TasteRefreshSchedule.IsDue(accountId, syncedAt, DateTime.UtcNow, minDays: 7, maxDays: 14),
            Is.False);
    }

    [Test]
    public void IsDue_ReturnsTrue_WhenPastMaxWindow()
    {
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var syncedAt = DateTime.UtcNow.AddDays(-20);

        Assert.That(
            TasteRefreshSchedule.IsDue(accountId, syncedAt, DateTime.UtcNow, minDays: 7, maxDays: 14),
            Is.True);
    }

    [Test]
    public void GetJitterDays_IsStableForSameAccount()
    {
        var accountId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var first = TasteRefreshSchedule.GetJitterDays(accountId, 7, 14);
        var second = TasteRefreshSchedule.GetJitterDays(accountId, 7, 14);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.InRange(0, 7));
    }

    [Test]
    public void GetJitterDays_SpreadsAcrossWindow()
    {
        var jitterValues = Enumerable.Range(0, 200)
            .Select(_ => TasteRefreshSchedule.GetJitterDays(Guid.NewGuid(), 7, 14))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        Assert.That(jitterValues.Count, Is.GreaterThan(1));
        Assert.That(jitterValues.Min(), Is.GreaterThanOrEqualTo(0));
        Assert.That(jitterValues.Max(), Is.LessThanOrEqualTo(7));
    }

    [Test]
    public void IsDue_RespectsPerUserJitter()
    {
        // Find two accounts with different jitter offsets
        Guid earlyId = default;
        Guid lateId = default;
        var foundEarly = false;
        var foundLate = false;

        for (var i = 0; i < 500 && !(foundEarly && foundLate); i++)
        {
            var id = Guid.NewGuid();
            var jitter = TasteRefreshSchedule.GetJitterDays(id, 7, 14);
            if (!foundEarly && jitter == 0)
            {
                earlyId = id;
                foundEarly = true;
            }

            if (!foundLate && jitter == 7)
            {
                lateId = id;
                foundLate = true;
            }
        }

        Assert.That(foundEarly && foundLate, Is.True, "Could not find accounts with jitter 0 and 7");

        var syncedAt = DateTime.UtcNow.AddDays(-7);
        var now = DateTime.UtcNow;

        Assert.That(TasteRefreshSchedule.IsDue(earlyId, syncedAt, now, 7, 14), Is.True);
        Assert.That(TasteRefreshSchedule.IsDue(lateId, syncedAt, now, 7, 14), Is.False);
    }
}