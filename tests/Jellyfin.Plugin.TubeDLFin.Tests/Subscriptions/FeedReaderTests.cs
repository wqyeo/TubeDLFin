using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Subscriptions;

public class FeedReaderTests
{
    private static readonly SubscriptionFeed Feed = new(Urls.Media("https://www.youtube.com/@BlenderOfficial/videos"), 50, NewestFirst: true);

    private readonly IMediaProber _prober = Substitute.For<IMediaProber>();
    private readonly FeedReader _reader;

    public FeedReaderTests()
    {
        _reader = new FeedReader(_prober);
    }

    [Fact]
    public async Task ReadAsync_ChannelTab_VideosWithArchiveKeysAndChannelName()
    {
        Probe("channel-videos");

        var listing = await _reader.ReadAsync(Feed, TestContext.Current.CancellationToken);

        Assert.Equal("Blender", listing.Name);
        Assert.Equal(5, listing.Entries.Count);
        Assert.Equal(("youtube sO_lhW8av3U", "https://www.youtube.com/watch?v=sO_lhW8av3U"), (listing.Entries[0].Key, listing.Entries[0].Url.AbsoluteUri));
        await _prober.Received(1).ProbeAsync(Feed.Url, false, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadAsync_Playlist_NamedAfterPlaylist()
    {
        Probe("playlist");

        var listing = await _reader.ReadAsync(Feed, TestContext.Current.CancellationToken);

        Assert.Equal("Blender Conference 2024", listing.Name);
        Assert.All(listing.Entries, e => Assert.StartsWith("youtube ", e.Key, System.StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("video", "single video")]
    [InlineData("channel-playlists", "lists playlists or tabs")]
    [InlineData("channel", "lists playlists or tabs")]
    public async Task ReadAsync_NotAListOfVideos_Refused(string sample, string message)
    {
        Probe(sample);

        var ex = await Assert.ThrowsAsync<ProbeFailedException>(() => _reader.ReadAsync(Feed, TestContext.Current.CancellationToken));

        Assert.Equal(ProbeFailure.Rejected, ex.Reason);
        Assert.Contains(message, ex.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsync_EmptyPlaylist_NoEntries()
    {
        _prober.ProbeAsync(Arg.Any<MediaUrl>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ProbeResult { Kind = ProbeKind.Playlist, Id = "PL1", Title = "New playlist" });

        Assert.Empty((await _reader.ReadAsync(Feed, TestContext.Current.CancellationToken)).Entries);
    }

    private void Probe(string sample) =>
        _prober.ProbeAsync(Arg.Any<MediaUrl>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ProbeResultParser().Parse(Samples.Read(sample), 50));
}
