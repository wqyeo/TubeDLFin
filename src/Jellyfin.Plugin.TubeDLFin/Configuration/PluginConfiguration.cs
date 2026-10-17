using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TubeDLFin.Configuration;

/// <summary>
/// The admin settings, stored by Jellyfin as XML. Saved data such as the download archive
/// lives in the plugin's data folder instead (see <see cref="Platform.IPluginPaths"/>).
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
}
