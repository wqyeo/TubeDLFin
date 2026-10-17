using System;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using MediaBrowser.Common.Plugins;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests.Configuration;

public class PluginConfigurationProviderTests
{
    [Fact]
    public void Current_PluginNotLoaded_ReturnsDefaults()
    {
        var pluginManager = Substitute.For<IPluginManager>();
        pluginManager.GetPlugin(Arg.Any<Guid>(), Arg.Any<Version?>()).Returns((LocalPlugin?)null);

        var settings = new PluginConfigurationProvider(pluginManager).Current;

        Assert.Equal(string.Empty, settings.YtDlpPath);
        Assert.Equal(string.Empty, settings.DenoPath);
    }
}
