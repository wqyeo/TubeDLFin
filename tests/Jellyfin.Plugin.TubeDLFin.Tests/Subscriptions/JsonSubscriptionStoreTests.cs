using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using static Jellyfin.Plugin.TubeDLFin.Tests.TestSupport.Subscriptions;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public sealed class JsonSubscriptionStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly JsonSubscriptionStore _store;
    private readonly string _file;

    public JsonSubscriptionStoreTests()
    {
        _file = _temp.Combine("data", "subscriptions.json");
        var paths = Substitute.For<IPluginPaths>();
        paths.SubscriptionsFile.Returns(_file);
        _store = new JsonSubscriptionStore(paths, new ManualTimeProvider(Created), NullLogger<JsonSubscriptionStore>.Instance);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task LoadAsync_NoFile_None()
    {
        Assert.Empty(await _store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var subscription = Create(baseline: [Entry("a").Key]) with
        {
            Profile = Profile with { Mode = DownloadMode.Audio, AudioFormat = AudioFormat.M4a, Subfolder = "YouTube" },
            Attempts = new Dictionary<string, int> { [Entry("b").Key] = 2 },
            LastCheckedAt = Created.AddHours(1),
            LastError = "Oops",
        };

        await _store.SaveAsync([subscription], TestContext.Current.CancellationToken);
        var loaded = Assert.Single(await _store.LoadAsync(TestContext.Current.CancellationToken));

        // Collections compare by reference in records: compare them separately.
        var sponsorBlock = loaded.Profile.SponsorBlock;
        Assert.Equal(subscription with { Baseline = loaded.Baseline, Attempts = loaded.Attempts, Profile = subscription.Profile with { SponsorBlock = sponsorBlock } }, loaded);
        Assert.Equal(subscription.Profile.SponsorBlock.Categories, sponsorBlock.Categories);
        Assert.Equal(subscription.Baseline, loaded.Baseline);
        Assert.Equal(subscription.Attempts, loaded.Attempts);
        Assert.False(File.Exists(_file + ".tmp"));

        var json = await File.ReadAllTextAsync(_file, TestContext.Current.CancellationToken);
        Assert.Contains("\"Version\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"Mode\": \"Audio\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_ReplacesPreviousFile()
    {
        await _store.SaveAsync([Create()], TestContext.Current.CancellationToken);
        await _store.SaveAsync([], TestContext.Current.CancellationToken);

        Assert.Empty(await _store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{ \"Version\": 2, \"Subscriptions\": [] }")]
    [InlineData("{ \"Subscriptions\": [] }")]
    public async Task LoadAsync_Unreadable_KeptAsideAndNone(string content)
    {
        _temp.WriteFile("data/subscriptions.json", content);

        Assert.Empty(await _store.LoadAsync(TestContext.Current.CancellationToken));

        Assert.False(File.Exists(_file));
        var aside = Assert.Single(Directory.GetFiles(_temp.Combine("data")));
        Assert.Equal("subscriptions.json.unreadable-20261018-080000", Path.GetFileName(aside));
        Assert.Equal(content, await File.ReadAllTextAsync(aside, TestContext.Current.CancellationToken));
    }
}
