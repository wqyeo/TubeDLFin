using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Installation;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Updates;

public sealed class ToolUpdateServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly IToolLocator _locator = Substitute.For<IToolLocator>();
    private readonly FakeReleaseClient _releases = new FakeReleaseClient()
        .WithLatest("yt-dlp/yt-dlp", "2026.08.19")
        .WithLatest("denoland/deno", "v2.9.7");

    private readonly IToolVersionProbe _probe = Substitute.For<IToolVersionProbe>();
    private readonly IToolInstaller _installer = Substitute.For<IToolInstaller>();
    private readonly ToolUpdateService _service;

    public ToolUpdateServiceTests()
    {
        _locator.Platform.Returns(Platforms.LinuxArm64);
        _locator.Locate(Arg.Any<ToolKind>()).Returns(call => Managed(call.Arg<ToolKind>()));

        // Null-tolerant: NSubstitute invokes this while a test re-configures the call.
        _installer.InstallAsync(default!, default!, default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(call => Task.FromResult(call.Arg<IToolDefinition>()?.VersionFromTag(call.ArgAt<string>(2)) ?? string.Empty));

        var registry = new ToolDefinitionRegistry([new YtDlpToolDefinition(), new DenoToolDefinition()]);
        _service = new ToolUpdateService(registry, _locator, _releases, _probe, _installer, TimeProvider.System, NullLogger<ToolUpdateService>.Instance);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task UpdateAsync_NothingInstalled_InstallsLatestOfEach()
    {
        var report = await _service.UpdateAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(
            [(ToolKind.YtDlp, ToolUpdateOutcome.Installed, "2026.08.19"), (ToolKind.Deno, ToolUpdateOutcome.Installed, "2.9.7")],
            report.Results.Select(r => (r.Kind, r.Outcome, r.Version)));
        Assert.Same(report, _service.LastReport);
        await _probe.DidNotReceiveWithAnyArgs().ProbeAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_LatestAlreadyInstalled_DoesNotDownload()
    {
        InstalledVersion(ToolKind.YtDlp, "2026.08.19");

        var report = await _service.UpdateAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(ToolUpdateOutcome.UpToDate, Result(report, ToolKind.YtDlp).Outcome);
        await _installer.DidNotReceive().InstallAsync(Arg.Is<IToolDefinition>(d => d.Kind == ToolKind.YtDlp), Arg.Any<ToolAsset>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_OlderInstalled_Updates()
    {
        InstalledVersion(ToolKind.Deno, "2.9.6");

        var result = Result(await _service.UpdateAsync(null, TestContext.Current.CancellationToken), ToolKind.Deno);

        Assert.Equal(ToolUpdateOutcome.Updated, result.Outcome);
        Assert.Equal("2.9.7", result.Version);
        Assert.Equal("Updated from 2.9.6.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_CustomPath_IsLeftAlone()
    {
        _locator.Locate(ToolKind.Deno).Returns(new ToolLocation(ToolKind.Deno, "/usr/bin/deno", IsCustom: true));

        var result = Result(await _service.UpdateAsync(null, TestContext.Current.CancellationToken), ToolKind.Deno);

        Assert.Equal(ToolUpdateOutcome.SkippedCustom, result.Outcome);
    }

    [Fact]
    public async Task UpdateAsync_UnsupportedPlatform_IsReported()
    {
        _locator.Platform.Returns(Platforms.LinuxMuslArm64);

        var report = await _service.UpdateAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(ToolUpdateOutcome.Installed, Result(report, ToolKind.YtDlp).Outcome);
        Assert.Equal(ToolUpdateOutcome.Unsupported, Result(report, ToolKind.Deno).Outcome);
    }

    [Fact]
    public async Task UpdateAsync_OneToolFails_OthersStillUpdate()
    {
        _installer.InstallAsync(Arg.Is<IToolDefinition>(d => d.Kind == ToolKind.YtDlp), Arg.Any<ToolAsset>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new ToolInstallException("Checksum mismatch")));

        var report = await _service.UpdateAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((ToolUpdateOutcome.Failed, "Checksum mismatch"), (Result(report, ToolKind.YtDlp).Outcome, Result(report, ToolKind.YtDlp).Message));
        Assert.Equal(ToolUpdateOutcome.Installed, Result(report, ToolKind.Deno).Outcome);
    }

    [Fact]
    public async Task UpdateAsync_WhileRunning_JoinsTheSameRun()
    {
        var gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _installer.InstallAsync(default!, default!, default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(gate.Task);

        var first = _service.UpdateAsync(null, TestContext.Current.CancellationToken);
        var second = _service.UpdateAsync(null, TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        await WaitUntil(() => _service.IsRunning);
        gate.SetResult("1");
        await first;
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task UpdateAsync_ReportsProgressPerTool()
    {
        var progress = new RecordingProgress();

        await _service.UpdateAsync(progress, TestContext.Current.CancellationToken);

        Assert.Equal([50.0, 100.0], progress.Values);
    }

    private static ToolUpdateResult Result(ToolUpdateReport report, ToolKind kind) => report.Results.Single(r => r.Kind == kind);

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }

    private ToolLocation Managed(ToolKind kind) => new(kind, _temp.Combine("bin", kind.ToString()), IsCustom: false);

    private void InstalledVersion(ToolKind kind, string version)
    {
        var path = _temp.WriteFile(Path.Combine("bin", kind.ToString()), "binary");
        _probe.ProbeAsync(Arg.Any<IToolDefinition>(), path, Arg.Any<CancellationToken>()).Returns(ToolProbeResult.Ok(version));
    }

    private sealed class RecordingProgress : IProgress<double>
    {
        public System.Collections.Generic.List<double> Values { get; } = [];

        public void Report(double value) => Values.Add(value);
    }
}
