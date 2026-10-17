using System;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Plugin.TubeDLFin.Configuration;

/// <summary>
/// Reads the settings from the plugin instance that Jellyfin's plugin manager created.
/// </summary>
public sealed class PluginConfigurationProvider : IPluginConfigurationProvider
{
    private readonly IPluginManager _pluginManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigurationProvider"/> class.
    /// </summary>
    /// <param name="pluginManager">Jellyfin's plugin manager.</param>
    public PluginConfigurationProvider(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
    }

    /// <inheritdoc />
    /// <remarks>Falls back to the defaults if the plugin instance isn't available (e.g. while it is being disabled).</remarks>
    public PluginConfiguration Current =>
        (_pluginManager.GetPlugin(PluginIdentity.Id)?.Instance as TubeDLFinPlugin)?.Configuration ?? new PluginConfiguration();
}
