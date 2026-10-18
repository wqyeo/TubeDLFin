using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public class NewUploadSelectorTests
{
    private static readonly HashSet<string> None = new(StringComparer.Ordinal);

    [Fact]
    public void Select_SkipsBaselineArchiveAndQueued_OldestFirst()
    {
        // Newest first, as a channel's Videos tab lists them.
        var listing = new[] { Entry("e"), Entry("d"), Entry("c"), Entry("b"), Entry("a") };
        var subscription = Create(baseline: [Entry("a").Key, Entry("b").Key]);
        var archive = new HashSet<string> { Entry("d").Key };
        var queued = new HashSet<string> { Entry("c").Url.AbsoluteUri };

        var picks = NewUploadSelector.Select(listing, subscription, archive, queued);

        Assert.Equal(["e"], picks.Select(Id));
    }

    [Fact]
    public void Select_NewestFirstFeed_ReversedSoOldestIsQueuedFirst()
    {
        var picks = NewUploadSelector.Select([Entry("c"), Entry("b"), Entry("a")], Create(), None, None);

        Assert.Equal(["a", "b", "c"], picks.Select(Id));
    }

    [Fact]
    public void Select_PlaylistFeed_KeepsOrder()
    {
        var picks = NewUploadSelector.Select([Entry("a"), Entry("b")], Create(newestFirst: false), None, None);

        Assert.Equal(["a", "b"], picks.Select(Id));
    }

    [Fact]
    public void Select_GivesUpAfterMaxAttempts()
    {
        var subscription = Create() with
        {
            Attempts = new Dictionary<string, int> { [Entry("a").Key] = Subscription.MaxAttempts, [Entry("b").Key] = Subscription.MaxAttempts - 1 },
        };

        var picks = NewUploadSelector.Select([Entry("b"), Entry("a")], subscription, None, None);

        Assert.Equal(["b"], picks.Select(Id));
    }

    [Fact]
    public void Select_AtMostMaxPerCheck_TheOldest()
    {
        var listing = Enumerable.Range(0, 25).Select(i => Entry($"v{24 - i:00}")).ToList(); // v24 (newest) ... v00

        var picks = NewUploadSelector.Select(listing, Create(), None, None);

        Assert.Equal(Enumerable.Range(0, NewUploadSelector.MaxPerCheck).Select(i => $"v{i:00}"), picks.Select(Id));
    }

    [Fact]
    public void Select_DuplicateEntries_Once()
    {
        Assert.Single(NewUploadSelector.Select([Entry("a"), Entry("a")], Create(), None, None));
    }

    private static string Id(FeedEntry entry) => entry.Key.Split(' ')[1];
}
