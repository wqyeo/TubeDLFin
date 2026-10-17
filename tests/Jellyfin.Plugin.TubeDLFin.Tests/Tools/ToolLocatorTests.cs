using System.IO;
using System.Runtime.InteropServices;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Tools;

public class ToolLocatorTests
{
    private readonly PluginConfiguration _settings = new();
    private readonly ToolLocator _locator;

    public ToolLocatorTests()
    {
        var configuration = Substitute.For<IPluginConfigurationProvider>();
        configuration.Current.Returns(_ => _settings);
        var paths = Substitute.For<IPluginPaths>();
        paths.BinDirectory.Returns(Path.Combine("data", "bin"));
        var environment = Substitute.For<IRuntimeEnvironment>();
        environment.IsLinux.Returns(true);
        environment.ProcessArchitecture.Returns(Architecture.Arm64);

        var registry = new ToolDefinitionRegistry([new YtDlpToolDefinition(), new DenoToolDefinition()]);
        _locator = new ToolLocator(registry, configuration, paths, environment);
    }

    [Fact]
    public void Platform_ComesFromEnvironment()
    {
        Assert.Equal("linux-arm64-glibc", _locator.Platform.Id);
    }

    [Theory]
    [InlineData(ToolKind.YtDlp, "yt-dlp")]
    [InlineData(ToolKind.Deno, "deno")]
    public void Locate_NoCustomPath_IsManagedCopyInBin(ToolKind kind, string executable)
    {
        Assert.Equal(new ToolLocation(kind, Path.Combine("data", "bin", executable), IsCustom: false), _locator.Locate(kind));
    }

    [Fact]
    public void Locate_CustomPath_IsUsedTrimmed()
    {
        _settings.DenoPath = "  /usr/local/bin/deno ";

        Assert.Equal(new ToolLocation(ToolKind.Deno, "/usr/local/bin/deno", IsCustom: true), _locator.Locate(ToolKind.Deno));
        Assert.False(_locator.Locate(ToolKind.YtDlp).IsCustom);
    }
}
