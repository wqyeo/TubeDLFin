using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Detection;

public sealed class ThrottledMediaProberTests : IDisposable
{
    private static readonly MediaUrl Url = Urls.Media("https://example.com/a");
    private static readonly ProbeResult Result = new() { Kind = ProbeKind.Video, Id = "a", Title = "A" };

    private readonly IMediaProber _inner = Substitute.For<IMediaProber>();
    private readonly ThrottledMediaProber _throttled;

    public ThrottledMediaProberTests()
    {
        _throttled = new ThrottledMediaProber(_inner, maxConcurrent: 1);
    }

    public void Dispose() => _throttled.Dispose();

    [Fact]
    public async Task ProbeAsync_SecondCallWaitsForFirst()
    {
        var first = new TaskCompletionSource<ProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _inner.ProbeAsync(Url, false, Arg.Any<CancellationToken>()).Returns(first.Task, Task.FromResult(Result));

        var running = _throttled.ProbeAsync(Url, false, TestContext.Current.CancellationToken);
        var waiting = _throttled.ProbeAsync(Url, false, TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.False(waiting.IsCompleted);
        await _inner.Received(1).ProbeAsync(Url, false, Arg.Any<CancellationToken>());

        first.SetResult(Result);
        Assert.Same(Result, await running);
        Assert.Same(Result, await waiting);
        await _inner.Received(2).ProbeAsync(Url, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProbeAsync_InnerFails_ReleasesSlot()
    {
        _inner.ProbeAsync(Url, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProbeResult>(new ProbeFailedException("no")), Task.FromResult(Result));

        await Assert.ThrowsAsync<ProbeFailedException>(() => _throttled.ProbeAsync(Url, false, TestContext.Current.CancellationToken));

        Assert.Same(Result, await _throttled.ProbeAsync(Url, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProbeAsync_CancelledWhileWaiting_Throws()
    {
        var blocker = new TaskCompletionSource<ProbeResult>();
        _inner.ProbeAsync(Url, false, Arg.Any<CancellationToken>()).Returns(blocker.Task);
        _ = _throttled.ProbeAsync(Url, false, TestContext.Current.CancellationToken);

        using var cancel = new CancellationTokenSource();
        var waiting = _throttled.ProbeAsync(Url, false, cancel.Token);
        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        blocker.SetResult(Result);
    }
}
