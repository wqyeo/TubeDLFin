using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Status;

public sealed class ToolVersionProbeTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly IProcessRunner _runner = Substitute.For<IProcessRunner>();
    private readonly ToolVersionProbe _probe;
    private readonly string _executable;

    public ToolVersionProbeTests()
    {
        _probe = new ToolVersionProbe(_runner);
        _executable = _temp.WriteFile("deno", "binary");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task ProbeAsync_MissingFile_FailsWithoutStartingIt()
    {
        var result = await _probe.ProbeAsync(new DenoToolDefinition(), _temp.Combine("missing"), TestContext.Current.CancellationToken);

        Assert.False(result.Works);
        Assert.Contains("File not found", result.Error, StringComparison.Ordinal);
        await _runner.DidNotReceiveWithAnyArgs().RunAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ProbeAsync_Success_RunsVersionArgumentsAndParses()
    {
        Returns(new ProcessResult(0, "deno 2.9.7 (stable)\n", string.Empty, TimedOut: false));

        var result = await _probe.ProbeAsync(new DenoToolDefinition(), _executable, TestContext.Current.CancellationToken);

        Assert.Equal(ToolProbeResult.Ok("2.9.7"), result);
        await _runner.Received(1).RunAsync(
            Arg.Is<ProcessRequest>(r => r.FileName == _executable && r.Arguments.Count == 1 && r.Arguments[0] == "--version" && r.Timeout != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProbeAsync_NonZeroExit_ReportsStderr()
    {
        Returns(new ProcessResult(127, string.Empty, "error while loading shared libraries: libc.so.6\n", TimedOut: false));

        var result = await _probe.ProbeAsync(new DenoToolDefinition(), _executable, TestContext.Current.CancellationToken);

        Assert.Equal("Deno exited with code 127: error while loading shared libraries: libc.so.6", result.Error);
    }

    [Fact]
    public async Task ProbeAsync_TimedOut_Fails()
    {
        Returns(new ProcessResult(-1, string.Empty, string.Empty, TimedOut: true));

        var result = await _probe.ProbeAsync(new YtDlpToolDefinition(), _executable, TestContext.Current.CancellationToken);

        Assert.Contains("did not answer", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProbeAsync_CannotStart_Fails()
    {
        _runner.RunAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromException<ProcessResult>(new Win32Exception("Permission denied")));

        var result = await _probe.ProbeAsync(new YtDlpToolDefinition(), _executable, TestContext.Current.CancellationToken);

        Assert.Contains("Permission denied", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProbeAsync_UnrecognisedOutput_Fails()
    {
        Returns(new ProcessResult(0, "something else", string.Empty, TimedOut: false));

        var result = await _probe.ProbeAsync(new DenoToolDefinition(), _executable, TestContext.Current.CancellationToken);

        Assert.Contains("Unrecognised", result.Error, StringComparison.Ordinal);
    }

    private void Returns(ProcessResult result) =>
        _runner.RunAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(result);
}
