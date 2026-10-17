using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Processes;

/// <summary>
/// Runs real processes through /bin/sh, so these only run on Linux and macOS.
/// </summary>
public class ProcessRunnerTests
{
    private readonly ProcessRunner _runner = new();

    public static bool IsUnix => !OperatingSystem.IsWindows();

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task RunAsync_CapturesOutputAndExitCode()
    {
        var result = await _runner.RunAsync(Shell("echo out; echo err >&2; exit 3"), TestContext.Current.CancellationToken);

        Assert.Equal((3, "out\n", "err\n", false), (result.ExitCode, result.StandardOutput, result.StandardError, result.TimedOut));
        Assert.False(result.Succeeded);
    }

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task RunAsync_ArgumentsAreNotShellParsed()
    {
        var result = await _runner.RunAsync(
            new ProcessRequest("/bin/sh", ["-c", "printf '%s|' \"$@\"", "sh", "a b", "$(whoami)", "; rm -rf /"]),
            TestContext.Current.CancellationToken);

        Assert.Equal("a b|$(whoami)|; rm -rf /|", result.StandardOutput);
    }

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task RunAsync_Timeout_KillsAndReportsTimedOut()
    {
        var watch = Stopwatch.StartNew();

        var result = await _runner.RunAsync(Shell("sleep 30") with { Timeout = TimeSpan.FromMilliseconds(200) }, TestContext.Current.CancellationToken);

        Assert.True(result.TimedOut);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10));
    }

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task RunAsync_Cancelled_KillsAndThrows()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _runner.RunAsync(Shell("sleep 30"), cts.Token));
    }

    [Fact]
    public async Task RunAsync_MissingExecutable_Throws()
    {
        await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(() =>
            _runner.RunAsync(new ProcessRequest("/definitely/not/here", []), TestContext.Current.CancellationToken));
    }

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task StreamAsync_HandsOverLinesFromBothStreams()
    {
        var lines = new System.Collections.Concurrent.ConcurrentQueue<ProcessOutputLine>();

        var exit = await _runner.StreamAsync(
            Shell("echo one; echo two >&2; printf 'three'; exit 2"),
            lines.Enqueue,
            TestContext.Current.CancellationToken);

        Assert.Equal(new ProcessExit(2, TimedOut: false), exit);
        Assert.Equal(
            new[] { new ProcessOutputLine("one", false), new ProcessOutputLine("three", false), new ProcessOutputLine("two", true) },
            lines.OrderBy(l => l.IsError).ThenBy(l => l.Text, StringComparer.Ordinal));
    }

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task StreamAsync_LinesArriveWhileRunning()
    {
        var firstLine = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var run = _runner.StreamAsync(Shell("echo started; sleep 30"), line => firstLine.TrySetResult(line.Text), cts.Token);

        Assert.Equal("started", await firstLine.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
    }

    [Fact(SkipUnless = nameof(IsUnix), Skip = "Needs /bin/sh")]
    public async Task StreamAsync_Timeout_ReportsTimedOut()
    {
        var exit = await _runner.StreamAsync(Shell("sleep 30") with { Timeout = TimeSpan.FromMilliseconds(200) }, _ => { }, TestContext.Current.CancellationToken);

        Assert.True(exit.TimedOut);
        Assert.False(exit.Succeeded);
    }

    private static ProcessRequest Shell(string script) => new("/bin/sh", ["-c", script]);
}
