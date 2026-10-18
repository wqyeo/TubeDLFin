using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public class SubscriptionTests
{
    private static readonly DateTimeOffset Later = Created.AddHours(7);

    [Fact]
    public void IsDue_AfterNextCheck_UnlessPaused()
    {
        var subscription = Create();

        Assert.False(subscription.IsDue(Created.AddHours(5)));
        Assert.True(subscription.IsDue(Created.AddHours(6)));
        Assert.False((subscription with { Paused = true }).IsDue(Later));
    }

    [Fact]
    public void After_RecordsCheckAndCountsAttempts()
    {
        var a = Entry("a").Key;
        var subscription = Create() with { LastError = "old", Attempts = new Dictionary<string, int> { [a] = 1 } };

        var after = subscription.After(new CheckOutcome(Later, null, [a, Entry("b").Key]));

        Assert.Equal((Later, (string?)null, Later.AddHours(6)), (after.LastCheckedAt!.Value, after.LastError, after.NextCheckAt));
        Assert.Equal(new Dictionary<string, int> { [a] = 2, [Entry("b").Key] = 1 }, after.Attempts);
        Assert.Equal(1, subscription.Attempts[a]); // unchanged
    }

    [Fact]
    public void After_RetryOnNextRun_StaysDue()
    {
        var after = Create().After(new CheckOutcome(Later, "yt-dlp is not installed yet.", []) { RetryOnNextRun = true });

        Assert.True(after.IsDue(Later));
        Assert.Equal("yt-dlp is not installed yet.", after.LastError);
    }

    [Fact]
    public void Counts_DownloadedAndGivenUp()
    {
        var subscription = Create() with
        {
            Attempts = new Dictionary<string, int>
            {
                [Entry("done").Key] = 2,
                [Entry("failed").Key] = Subscription.MaxAttempts,
                [Entry("retrying").Key] = 1,
            },
        };
        var archive = new HashSet<string> { Entry("done").Key, Entry("manual").Key };

        Assert.Equal((1, 1), (subscription.CountDownloaded(archive), subscription.CountGivenUp(archive)));
    }
}
