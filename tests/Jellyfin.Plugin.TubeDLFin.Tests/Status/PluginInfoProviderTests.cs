using System.Runtime.InteropServices;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Status;
using MediaBrowser.Controller;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Status;

public class PluginInfoProviderTests
{
    [Fact]
    public void GetInfo_CombinesServerPathsAndEnvironment()
    {
        var serverHost = Substitute.For<IServerApplicationHost>();
        serverHost.ApplicationVersionString.Returns("12.2.0");
        var paths = Substitute.For<IPluginPaths>();
        paths.DataRoot.Returns("/config/data/tubedlfin");
        var environment = Substitute.For<IRuntimeEnvironment>();
        environment.OperatingSystemDescription.Returns("Debian GNU/Linux 13");
        environment.ProcessArchitecture.Returns(Architecture.Arm64);

        var info = new PluginInfoProvider(serverHost, paths, environment).GetInfo();

        Assert.Equal("12.2.0", info.ServerVersion);
        Assert.Equal("/config/data/tubedlfin", info.DataPath);
        Assert.Equal("Debian GNU/Linux 13", info.OperatingSystem);
        Assert.Equal("Arm64", info.Architecture);
        Assert.Equal(typeof(TubeDLFinPlugin).Assembly.GetName().Version!.ToString(), info.PluginVersion);
    }
}
