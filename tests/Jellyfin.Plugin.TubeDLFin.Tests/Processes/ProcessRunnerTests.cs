using System;
using System.Diagnostics;
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

    private static ProcessRequest Shell(string script) => new("/bin/sh", ["-c", script]);
}
