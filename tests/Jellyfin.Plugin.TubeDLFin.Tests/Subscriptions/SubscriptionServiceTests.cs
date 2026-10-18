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
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public sealed class SubscriptionServiceTests : IDisposable
{
    private const string Link = "https://www.youtube.com/@BlenderOfficial";

    private readonly MemoryStore _store = new();
    private readonly IFeedReader _reader = Substitute.For<IFeedReader>();
    private readonly ISubscriptionChecker _checker = Substitute.For<ISubscriptionChecker>();
    private readonly IDownloadArchive _archive = Substitute.For<IDownloadArchive>();
    private readonly ILibraryCatalog _libraries = Substitute.For<ILibraryCatalog>();
    private readonly ManualTimeProvider _time = new(Created);
    private readonly DownloadManager _downloads;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        _downloads = new DownloadManager(_time);
        _archive.Read(Arg.Any<string>()).Returns(new HashSet<string>());
        _libraries.GetLibraries().Returns([DownloadPlans.Movies]);
        _reader.ReadAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new FeedListing("Blender", [Entry("a"), Entry("b"), Entry("a")]));
        _service = new SubscriptionService(
            _store,
            new SubscriptionValidator(new DownloadRequestValidator()),
            new SubscriptionFeedLocator([new YouTubeFeedResolver()]),
            _reader,
            _checker,
            _archive,
            _downloads,
            _libraries,
            _time,
            NullLogger<SubscriptionService>.Instance);
    }

    public void Dispose()
    {
        _service.Dispose();
        _downloads.Dispose();
    }

    [Fact]
    public async Task AddAsync_ListsFeedRecordsBaselineAndSaves()
    {
        var result = await Add();

        var info = Assert.IsType<SubscriptionInfo>(result.Subscription);
        Assert.Equal(("Blender", "https://www.youtube.com/@BlenderOfficial/videos", "Movies", Created.AddHours(6)), (info.Name, info.Url.AbsoluteUri, info.LibraryName, info.NextCheckAt));
        var saved = Assert.Single(_store.Saved);
        Assert.Equal([Entry("a").Key, Entry("b").Key], saved.Baseline);
        Assert.Equal((50, true), (saved.ListLimit, saved.NewestFirst));
        await _reader.Received(1).ReadAsync(Arg.Is<SubscriptionFeed>(f => f.Url.Value.AbsoluteUri == info.Url.AbsoluteUri), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_OwnName_Kept()
    {
        Assert.Equal("Blender talks", (await Add(new SubscriptionRequest { Url = Link, Name = "Blender talks", Profile = Profile })).Subscription?.Name);
    }

    [Fact]
    public async Task AddAsync_Invalid_NothingListedOrSaved()
    {
        var result = await Add(new SubscriptionRequest { Url = Link, IntervalHours = 0, Profile = Profile });

        Assert.Equal(["Check every 1 to 168 hours."], result.Errors);
        await _reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, TestContext.Current.CancellationToken);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task AddAsync_SameFeedAndLibrary_Refused()
    {
        await Add();

        // The channel's main page and its Videos tab are the same feed.
        var again = await Add(new SubscriptionRequest { Url = Link + "/videos", Profile = Profile });

        Assert.Equal(["You are already subscribed to this in that library."], again.Errors);
        Assert.Single(_store.Saved);
    }

    [Fact]
    public async Task AddAsync_FeedRefused_ThrowsAndSavesNothing()
    {
        _reader.ReadAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromException<FeedListing>(new ProbeFailedException(ProbeFailure.Rejected, "This channel does not exist.")));

        await Assert.ThrowsAsync<ProbeFailedException>(() => Add());
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task AddAsync_SaveFails_NotKept()
    {
        _store.Fail = true;

        await Assert.ThrowsAsync<IOException>(() => Add());

        _store.Fail = false;
        Assert.Empty(await _service.GetAllAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAllAsync_LoadsOnceSortsByNameAndCounts()
    {
        var withError = Create() with { Id = Guid.NewGuid(), Name = "b", Attempts = new Dictionary<string, int> { [Entry("x").Key] = 1, [Entry("y").Key] = 1 } };
        _store.Saved = [withError, Create() with { Name = "A", Profile = Profile with { LibraryId = "gone" } }];
        _archive.Read(DownloadPlans.Movies.Id).Returns(new HashSet<string> { Entry("x").Key });
        _downloads.Enqueue(DownloadPlans.Create(), DownloadSources.Subscription(withError.Id));

        var all = await _service.GetAllAsync(TestContext.Current.CancellationToken);
        await _service.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["A", "b"], all.Select(s => s.Name));
        Assert.Null(all[0].LibraryName);
        Assert.Equal((1, 1, 0), (all[1].Downloaded, all[1].Queued, all[1].GivenUp));
        Assert.Equal(1, _store.LoadCount);
    }

    [Fact]
    public async Task UpdateAsync_NewIntervalMovesNextCheck()
    {
        var id = (await Add()).Subscription!.Id;

        var result = await _service.UpdateAsync(id, new SubscriptionUpdate { Name = "Blender Studio", IntervalHours = 24 }, TestContext.Current.CancellationToken);

        Assert.Equal(("Blender Studio", 24, Created.AddHours(24)), (result.Subscription?.Name, result.Subscription?.IntervalHours, result.Subscription?.NextCheckAt));
        Assert.Equal("Blender Studio", Assert.Single(_store.Saved).Name);
    }

    [Fact]
    public async Task UpdateAsync_InvalidOrMissing()
    {
        var id = (await Add()).Subscription!.Id;

        Assert.Equal(["Enter a name."], (await _service.UpdateAsync(id, new SubscriptionUpdate { Name = " " }, TestContext.Current.CancellationToken)).Errors);
        Assert.True((await _service.UpdateAsync(Guid.NewGuid(), new SubscriptionUpdate(), TestContext.Current.CancellationToken)).IsNotFound);
    }

    [Fact]
    public async Task SetPausedAndDelete()
    {
        var id = (await Add()).Subscription!.Id;

        Assert.True((await _service.SetPausedAsync(id, paused: true, TestContext.Current.CancellationToken)).Subscription?.Paused);
        Assert.False((await _service.SetPausedAsync(id, paused: false, TestContext.Current.CancellationToken)).Subscription?.Paused);
        Assert.True(await _service.DeleteAsync(id, TestContext.Current.CancellationToken));
        Assert.False(await _service.DeleteAsync(id, TestContext.Current.CancellationToken));
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task CheckNowAsync_RecordsOutcome()
    {
        var id = (await Add()).Subscription!.Id;
        _time.Advance(TimeSpan.FromHours(1));
        _checker.CheckAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new CheckOutcome(_time.GetUtcNow(), null, [Entry("c").Key]));

        var info = (await _service.CheckNowAsync(id, TestContext.Current.CancellationToken)).Subscription;

        Assert.Equal((_time.GetUtcNow(), _time.GetUtcNow().AddHours(6)), (info?.LastCheckedAt, info?.NextCheckAt));
        Assert.Equal(1, Assert.Single(_store.Saved).Attempts[Entry("c").Key]);
        Assert.True((await _service.CheckNowAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)).IsNotFound);
    }

    [Fact]
    public async Task CheckDueAsync_OnlyDueAndNotPaused()
    {
        var due = Create() with { Id = Guid.NewGuid(), NextCheckAt = Created };
        _store.Saved = [due, Create() with { NextCheckAt = Created.AddHours(1) }, Create() with { Id = Guid.NewGuid(), NextCheckAt = Created, Paused = true }];
        _checker.CheckAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(call => new CheckOutcome(Created, null, []));
        var progress = new List<double>();

        await _service.CheckDueAsync(new SynchronousProgress(progress), TestContext.Current.CancellationToken);

        await _checker.Received(1).CheckAsync(Arg.Is<Subscription>(s => s.Id == due.Id), Arg.Any<CancellationToken>());
        Assert.Equal(100, progress[^1]);
        Assert.Equal(Created, _store.Saved.Single(s => s.Id == due.Id).LastCheckedAt);
    }

    [Fact]
    public async Task CheckDueAsync_UnexpectedError_RecordedAndOthersChecked()
    {
        var first = Create() with { Id = Guid.NewGuid(), Name = "first", NextCheckAt = Created };
        var second = Create() with { Id = Guid.NewGuid(), Name = "second", NextCheckAt = Created };
        _store.Saved = [first, second];
        _checker.CheckAsync(Arg.Is<Subscription>(s => s.Id == first.Id), Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("boom"));
        _checker.CheckAsync(Arg.Is<Subscription>(s => s.Id == second.Id), Arg.Any<CancellationToken>()).Returns(new CheckOutcome(Created, null, []));

        await _service.CheckDueAsync(new SynchronousProgress([]), TestContext.Current.CancellationToken);

        Assert.Equal("Unexpected error: boom", _store.Saved.Single(s => s.Id == first.Id).LastError);
        Assert.Equal(Created, _store.Saved.Single(s => s.Id == second.Id).LastCheckedAt);
    }

    [Fact]
    public async Task CheckNowAsync_DeletedDuringCheck_NotRecreated()
    {
        var id = (await Add()).Subscription!.Id;
        _checker.CheckAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(async _ =>
        {
            await _service.DeleteAsync(id, CancellationToken.None);
            return new CheckOutcome(Created, null, []);
        });

        Assert.True((await _service.CheckNowAsync(id, TestContext.Current.CancellationToken)).IsNotFound);
        Assert.Empty(_store.Saved);
    }

    private Task<SubscriptionResult> Add(SubscriptionRequest? request = null) =>
        _service.AddAsync(request ?? new SubscriptionRequest { Url = Link, Profile = Profile }, TestContext.Current.CancellationToken);

    private sealed class MemoryStore : ISubscriptionStore
    {
        public IReadOnlyList<Subscription> Saved { get; set; } = [];

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public bool Fail { get; set; }

        public Task<IReadOnlyList<Subscription>> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCount++;
            return Task.FromResult(Saved);
        }

        public Task SaveAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new IOException("Disk full");
            }

            SaveCount++;
            Saved = subscriptions.ToList();
            return Task.CompletedTask;
        }
    }

    // Progress<T> reports on the thread pool; this records reports as they happen.
    private sealed class SynchronousProgress(List<double> reports) : IProgress<double>
    {
        public void Report(double value) => reports.Add(value);
    }
}
