using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Status;

public sealed class ToolStatusServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly IToolLocator _locator = Substitute.For<IToolLocator>();
    private readonly IToolVersionProbe _probe = Substitute.For<IToolVersionProbe>();
    private readonly IFfmpegLocator _ffmpeg = Substitute.For<IFfmpegLocator>();
    private readonly IToolUpdateService _updates = Substitute.For<IToolUpdateService>();
    private readonly ToolStatusService _service;

    public ToolStatusServiceTests()
    {
        _locator.Platform.Returns(Platforms.LinuxX64);
        _locator.Locate(Arg.Any<ToolKind>()).Returns(call => new ToolLocation(call.Arg<ToolKind>(), _temp.Combine(call.Arg<ToolKind>().ToString()), IsCustom: false));
        _ffmpeg.GetStatus().Returns(new FfmpegStatus("/usr/lib/jellyfin-ffmpeg/ffmpeg", "/usr/lib/jellyfin-ffmpeg/ffprobe", "7.1.1"));

        var registry = new ToolDefinitionRegistry([new YtDlpToolDefinition(), new DenoToolDefinition()]);
        _service = new ToolStatusService(registry, _locator, _probe, _ffmpeg, _updates);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task GetOverviewAsync_NothingInstalled_IsNotInstalled()
    {
        var overview = await _service.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal("linux-x64-glibc", overview.Platform);
        Assert.All(overview.Tools, t => Assert.Equal(ToolState.NotInstalled, t.State));
        Assert.True(overview.Ffmpeg.Found);
    }

    [Fact]
    public async Task GetOverviewAsync_Installed_ReportsVersionOrError()
    {
        Install(ToolKind.YtDlp, ToolProbeResult.Ok("2026.08.19"));
        Install(ToolKind.Deno, ToolProbeResult.Failed("Exec format error"));

        var overview = await _service.GetOverviewAsync(TestContext.Current.CancellationToken);

        var ytDlp = overview.Tools.Single(t => t.Kind == ToolKind.YtDlp);
        var deno = overview.Tools.Single(t => t.Kind == ToolKind.Deno);
        Assert.Equal((ToolState.Ready, "2026.08.19"), (ytDlp.State, ytDlp.Version));
        Assert.Equal((ToolState.Broken, "Exec format error"), (deno.State, deno.Message));
    }

    [Fact]
    public async Task GetOverviewAsync_NoBuildForPlatform_IsUnsupported()
    {
        _locator.Platform.Returns(Platforms.LinuxMuslX64);

        var overview = await _service.GetOverviewAsync(TestContext.Current.CancellationToken);

        var deno = overview.Tools.Single(t => t.Kind == ToolKind.Deno);
        Assert.Equal(ToolState.Unsupported, deno.State);
        Assert.Contains("linux-x64-musl", deno.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetOverviewAsync_CustomPath_IsProbedEvenIfMissing()
    {
        _locator.Locate(ToolKind.Deno).Returns(new ToolLocation(ToolKind.Deno, "/nope/deno", IsCustom: true));
        _probe.ProbeAsync(Arg.Any<IToolDefinition>(), "/nope/deno", Arg.Any<CancellationToken>()).Returns(ToolProbeResult.Failed("File not found: /nope/deno"));

        var deno = (await _service.GetOverviewAsync(TestContext.Current.CancellationToken)).Tools.Single(t => t.Kind == ToolKind.Deno);

        Assert.Equal((ToolState.Broken, true), (deno.State, deno.IsCustom));
    }

    [Fact]
    public async Task GetOverviewAsync_IncludesUpdateState()
    {
        var report = new ToolUpdateReport(DateTimeOffset.UnixEpoch, []);
        _updates.IsRunning.Returns(true);
        _updates.LastReport.Returns(report);

        var overview = await _service.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.True(overview.IsUpdating);
        Assert.Same(report, overview.LastUpdate);
    }

    private void Install(ToolKind kind, ToolProbeResult result)
    {
        var path = _temp.WriteFile(kind.ToString(), "binary");
        _probe.ProbeAsync(Arg.Any<IToolDefinition>(), path, Arg.Any<CancellationToken>()).Returns(result);
    }
}
