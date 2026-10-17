using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Status;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TubeDLFin.Tests;

public class PluginServiceRegistratorTests
{
    [Fact]
    public void RegisterServices_EveryServiceResolves()
    {
        var services = new ServiceCollection();

        // Provided by Jellyfin in production.
        var serverHost = Substitute.For<IServerApplicationHost>();
        services.AddSingleton(serverHost);
        services.AddSingleton(Substitute.For<IApplicationPaths>());

        new PluginServiceRegistrator().RegisterServices(services, serverHost);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        Assert.IsType<PluginInfoProvider>(provider.GetRequiredService<IPluginInfoProvider>());
        Assert.IsType<PluginPaths>(provider.GetRequiredService<IPluginPaths>());
        Assert.IsType<RuntimeEnvironment>(provider.GetRequiredService<IRuntimeEnvironment>());
    }
}
