using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public sealed class SubscriptionCheckerTests : IDisposable
{
    private static readonly DateTimeOffset Now = Created.AddHours(6);

    private readonly IFeedReader _feeds = Substitute.For<IFeedReader>();
    private readonly IDownloadArchive _archive = Substitute.For<IDownloadArchive>();
    private readonly ILibraryCatalog _libraries = Substitute.For<ILibraryCatalog>();
    private readonly DownloadManager _downloads = new(new ManualTimeProvider(Now));
    private readonly SubscriptionChecker _checker;
    private readonly Subscription _subscription = Create(baseline: [Entry("a").Key]);

    public SubscriptionCheckerTests()
    {
        _archive.Read(Arg.Any<string>()).Returns(new HashSet<string>());
        _libraries.GetLibraries().Returns([DownloadPlans.Movies]);
        _checker = new SubscriptionChecker(
            _feeds,
            _archive,
            _downloads,
            new DownloadRequestValidator(),
            _libraries,
            new ManualTimeProvider(Now),
            NullLogger<SubscriptionChecker>.Instance);
    }

    public void Dispose() => _downloads.Dispose();

    [Fact]
    public async Task CheckAsync_QueuesNewUploadsAsSingleVideos()
    {
        Feed(Entry("c"), Entry("b"), Entry("a"));

        var outcome = await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        Assert.Equal(new CheckOutcome(Now, null, outcome.QueuedKeys), outcome);
        Assert.Equal([Entry("b").Key, Entry("c").Key], outcome.QueuedKeys);
        var jobs = _downloads.GetJobs();
        Assert.Equal(["Video b", "Video c"], jobs.Select(j => j.Title).Order()); // same clock time: list order is arbitrary
        Assert.All(jobs, j => Assert.Equal(DownloadSources.Subscription(_subscription.Id), j.Source));
        Assert.Equal("/media/movies", jobs[0].Destination);
        await _feeds.Received(1).ReadAsync(
            Arg.Is<SubscriptionFeed>(f => f.Url.Value == _subscription.Url && f.ListLimit == 50 && f.NewestFirst),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_AlreadyQueued_NotQueuedAgain()
    {
        Feed(Entry("b"));
        await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        var second = await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        Assert.Empty(second.QueuedKeys);
        Assert.Single(_downloads.GetJobs());
    }

    [Fact]
    public async Task CheckAsync_NothingNew_NoError()
    {
        Feed(Entry("a"));

        Assert.Equal(new CheckOutcome(Now, null, []), await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken) with { QueuedKeys = [] });
        Assert.Empty(_downloads.GetJobs());
    }

    [Fact]
    public async Task CheckAsync_LibraryGone_ErrorAndNothingQueued()
    {
        Feed(Entry("b"));
        _libraries.GetLibraries().Returns([]);

        var outcome = await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        Assert.Equal("Choose a library.", outcome.Error);
        Assert.Empty(outcome.QueuedKeys);
        Assert.Empty(_downloads.GetJobs());
    }

    [Fact]
    public async Task CheckAsync_ToolNotReady_RetryOnNextRun()
    {
        _feeds.ReadAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromException<FeedListing>(new ToolNotReadyException("yt-dlp is not installed yet.")));

        var outcome = await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        Assert.True(outcome.RetryOnNextRun);
        Assert.Equal("yt-dlp is not installed yet.", outcome.Error);
    }

    [Fact]
    public async Task CheckAsync_SiteRefused_ErrorUntilNextInterval()
    {
        _feeds.ReadAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromException<FeedListing>(new ProbeFailedException(ProbeFailure.Rejected, "This channel does not exist.")));

        var outcome = await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        Assert.Equal(("This channel does not exist.", false), (outcome.Error, outcome.RetryOnNextRun));
    }

    [Fact]
    public async Task CheckAsync_ArchiveUnreadable_Error()
    {
        Feed(Entry("b"));
        _archive.Read(Arg.Any<string>()).Throws(new IOException("Input/output error"));

        var outcome = await _checker.CheckAsync(_subscription, TestContext.Current.CancellationToken);

        Assert.Equal("Could not read the download archive: Input/output error", outcome.Error);
    }

    private void Feed(params FeedEntry[] entries) =>
        _feeds.ReadAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new FeedListing("Blender", entries));
}
