using System;
using System.IO;
using Jellyfin.Plugin.TubeDLFin.Platform;
using MediaBrowser.Common.Configuration;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Platform;

public class PluginPathsTests
{
    [Fact]
    public void DataRoot_IsInsideServerDataFolder()
    {
        var applicationPaths = Substitute.For<IApplicationPaths>();
        applicationPaths.DataPath.Returns(Path.Combine("config", "data"));

        var paths = new PluginPaths(applicationPaths);

        Assert.Equal(Path.Combine("config", "data", "tubedlfin"), paths.DataRoot);
        Assert.Equal(Path.Combine("config", "data", "tubedlfin", "tmp", "downloads"), paths.DownloadsTempDirectory);
        Assert.Equal(Path.Combine("config", "data", "tubedlfin", "archive"), paths.ArchiveDirectory);
    }

    [Fact]
    public void Constructor_NullApplicationPaths_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PluginPaths(null!));
    }
}
