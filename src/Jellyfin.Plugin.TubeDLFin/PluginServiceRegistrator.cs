using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Status;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TubeDLFin;

/// <summary>
/// Registers the plugin's services with Jellyfin's dependency injection container.
/// This is the plugin's composition root: classes depend on interfaces, and this is the
/// only place that picks the implementations.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IRuntimeEnvironment, RuntimeEnvironment>();
        serviceCollection.AddSingleton<IPluginPaths, PluginPaths>();
        serviceCollection.AddSingleton<IPluginInfoProvider, PluginInfoProvider>();
    }
}
