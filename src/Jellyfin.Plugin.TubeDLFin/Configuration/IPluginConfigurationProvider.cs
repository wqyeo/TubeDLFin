namespace Jellyfin.Plugin.TubeDLFin.Configuration;

/// <summary>
/// Gives services the current admin settings without reaching for a static plugin instance.
/// </summary>
public interface IPluginConfigurationProvider
{
    /// <summary>
    /// Gets the settings as currently saved.
    /// </summary>
    PluginConfiguration Current { get; }
}
