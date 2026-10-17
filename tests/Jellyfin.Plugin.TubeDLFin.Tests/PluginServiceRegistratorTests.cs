using System;
using System.Linq;
using System.Net.Http;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Downloads.Options;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        services.AddSingleton(Substitute.For<IPluginManager>());
        services.AddSingleton(Substitute.For<IMediaEncoder>());
        services.AddSingleton(Substitute.For<IHttpClientFactory>());
        services.AddSingleton(Substitute.For<ILibraryManager>());
        services.AddSingleton(Substitute.For<ILibraryMonitor>());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        new PluginServiceRegistrator().RegisterServices(services, serverHost);
        var ours = services.Where(d => d.ServiceType.Namespace?.StartsWith("Jellyfin.Plugin.TubeDLFin", StringComparison.Ordinal) == true).ToList();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        Assert.All(ours, d => Assert.NotNull(provider.GetRequiredService(d.ServiceType)));
        Assert.Equal(2, provider.GetRequiredService<IToolDefinitionRegistry>().All.Count);
        Assert.IsType<CachingToolVersionProbe>(provider.GetRequiredService<IToolVersionProbe>());
        Assert.IsType<ThrottledMediaProber>(provider.GetRequiredService<IMediaProber>());
        Assert.Same(provider.GetRequiredService<IDownloadManager>(), provider.GetRequiredService<IDownloadJobSource>());
        Assert.Contains(provider.GetServices<IHostedService>(), s => s is DownloadDispatcher);
        Assert.Equal(9, provider.GetServices<IDownloadOptionSet>().Count());
        Assert.IsType<LibraryRefreshStep>(provider.GetServices<IFinishedItemStep>().Last());
        Assert.NotNull(new UpdateToolsTask(provider.GetRequiredService<IToolUpdateService>()));
    }
}
