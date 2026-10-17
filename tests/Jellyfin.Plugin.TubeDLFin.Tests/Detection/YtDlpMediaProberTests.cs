using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Detection;

public class YtDlpMediaProberTests
{
    private static readonly MediaUrl Url = Urls.Media("https://www.youtube.com/watch?v=sO_lhW8av3U&list=PL1");

    private readonly IYtDlpCommandFactory _commands = Substitute.For<IYtDlpCommandFactory>();
    private readonly IProcessRunner _runner = Substitute.For<IProcessRunner>();
    private readonly IPlaylistLinkInspector _inspector = Substitute.For<IPlaylistLinkInspector>();
    private readonly YtDlpMediaProber _prober;
    private IReadOnlyList<string> _options = [];
    private ProcessRequest? _started;

    public YtDlpMediaProberTests()
    {
        _commands.CreateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<MediaUrl>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _options = call.Arg<IEnumerable<string>>()?.ToList() ?? [];
                return new ProcessRequest("yt-dlp", [.. _options, "--", call.Arg<MediaUrl>()?.ToString() ?? string.Empty]);
            });
        _runner.RunAsync(Arg.Do<ProcessRequest>(r => _started = r), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, Samples.Read("video"), string.Empty, TimedOut: false));

        _prober = new YtDlpMediaProber(_commands, _runner, new ProbeResultParser(), [_inspector], new YtDlpErrorExplainer(), NullLogger<YtDlpMediaProber>.Instance);
    }

    [Fact]
    public async Task ProbeAsync_ListsFlatWithLimitAndTimeout()
    {
        var result = await _prober.ProbeAsync(Url, noPlaylist: false, TestContext.Current.CancellationToken);

        Assert.Equal(["--dump-single-json", "--flat-playlist", "--playlist-items", "1:100"], _options);
        Assert.Equal(TimeSpan.FromMinutes(2), _started?.Timeout);
        Assert.Equal("Me at the zoo", result.Title);
    }

    [Fact]
    public async Task ProbeAsync_NoPlaylist_AddsOption()
    {
        await _prober.ProbeAsync(Url, noPlaylist: true, TestContext.Current.CancellationToken);

        Assert.Equal("--no-playlist", _options[^1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProbeAsync_ReportsPlaylistChoice(bool hasChoice)
    {
        _inspector.HasPlaylistChoice(Url).Returns(hasChoice);

        var result = await _prober.ProbeAsync(Url, noPlaylist: false, TestContext.Current.CancellationToken);

        Assert.Equal(hasChoice, result.HasPlaylistChoice);
    }

    [Theory]
    [InlineData("null\n")]
    [InlineData("")]
    public async Task ProbeAsync_Refused_ThrowsWithYtDlpsError(string stdout)
    {
        Run(1, stdout, "ERROR: [youtube] abc: Video unavailable. This video is private\n");

        var ex = await Assert.ThrowsAsync<ProbeFailedException>(() => _prober.ProbeAsync(Url, false, TestContext.Current.CancellationToken));

        Assert.Equal(ProbeFailure.Rejected, ex.Reason);
        Assert.Equal("[youtube] abc: Video unavailable. This video is private", ex.Message);
    }

    [Fact]
    public async Task ProbeAsync_RefusedWithoutMessage_MentionsExitCode()
    {
        Run(2, "null", string.Empty);

        var ex = await Assert.ThrowsAsync<ProbeFailedException>(() => _prober.ProbeAsync(Url, false, TestContext.Current.CancellationToken));

        Assert.Contains("exited with code 2", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProbeAsync_TimedOut_Throws()
    {
        _runner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).Returns(new ProcessResult(-1, string.Empty, string.Empty, TimedOut: true));

        var ex = await Assert.ThrowsAsync<ProbeFailedException>(() => _prober.ProbeAsync(Url, false, TestContext.Current.CancellationToken));

        Assert.Equal(ProbeFailure.TimedOut, ex.Reason);
    }

    [Fact]
    public async Task ProbeAsync_GarbageOutput_ThrowsUnreadable()
    {
        Run(0, "Traceback (most recent call last):", string.Empty);

        var ex = await Assert.ThrowsAsync<ProbeFailedException>(() => _prober.ProbeAsync(Url, false, TestContext.Current.CancellationToken));

        Assert.Equal(ProbeFailure.UnreadableOutput, ex.Reason);
    }

    [Fact]
    public async Task ProbeAsync_PartialFailure_ReturnsResultWithWarnings()
    {
        Run(1, Samples.Read("playlist"), "WARNING: [youtube] Some formats are missing\nERROR: [youtube] xyz: Video unavailable\n");

        var result = await _prober.ProbeAsync(Url, false, TestContext.Current.CancellationToken);

        Assert.Equal(ProbeKind.Playlist, result.Kind);
        Assert.Equal(["[youtube] Some formats are missing", "[youtube] xyz: Video unavailable"], result.Warnings);
    }

    [Fact]
    public async Task ProbeAsync_CannotStart_ThrowsToolNotReady()
    {
        _runner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).ThrowsAsync(new Win32Exception("Permission denied"));

        var ex = await Assert.ThrowsAsync<ToolNotReadyException>(() => _prober.ProbeAsync(Url, false, TestContext.Current.CancellationToken));

        Assert.Contains("Permission denied", ex.Message, StringComparison.Ordinal);
    }

    private void Run(int exitCode, string stdout, string stderr) =>
        _runner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).Returns(new ProcessResult(exitCode, stdout, stderr, TimedOut: false));
}
