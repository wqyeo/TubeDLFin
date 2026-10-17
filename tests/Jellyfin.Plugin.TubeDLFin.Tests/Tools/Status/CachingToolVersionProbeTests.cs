using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools.Status;

public sealed class CachingToolVersionProbeTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly IToolVersionProbe _inner = Substitute.For<IToolVersionProbe>();
    private readonly CachingToolVersionProbe _probe;
    private readonly IToolDefinition _definition = new YtDlpToolDefinition();

    public CachingToolVersionProbeTests()
    {
        _probe = new CachingToolVersionProbe(_inner);
        _inner.ProbeAsync(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(ToolProbeResult.Ok("1"), ToolProbeResult.Ok("2"));
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task ProbeAsync_UnchangedFile_UsesCache()
    {
        var path = _temp.WriteFile("yt-dlp", "v1");

        await _probe.ProbeAsync(_definition, path, TestContext.Current.CancellationToken);
        var second = await _probe.ProbeAsync(_definition, path, TestContext.Current.CancellationToken);

        Assert.Equal("1", second.Version);
        await _inner.Received(1).ProbeAsync(_definition, path, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProbeAsync_ReplacedFile_ProbesAgain()
    {
        var path = _temp.WriteFile("yt-dlp", "v1");
        await _probe.ProbeAsync(_definition, path, TestContext.Current.CancellationToken);

        await File.WriteAllTextAsync(path, "version two", TestContext.Current.CancellationToken);
        var second = await _probe.ProbeAsync(_definition, path, TestContext.Current.CancellationToken);

        Assert.Equal("2", second.Version);
    }

    [Fact]
    public async Task ProbeAsync_MissingFile_IsNotCached()
    {
        var path = _temp.Combine("missing");

        await _probe.ProbeAsync(_definition, path, TestContext.Current.CancellationToken);
        await _probe.ProbeAsync(_definition, path, TestContext.Current.CancellationToken);

        await _inner.Received(2).ProbeAsync(_definition, path, Arg.Any<CancellationToken>());
    }
}
