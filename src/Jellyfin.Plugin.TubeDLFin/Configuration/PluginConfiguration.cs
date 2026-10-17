using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TubeDLFin.Configuration;

/// <summary>
/// The admin settings, stored by Jellyfin as XML. Saved data such as the download archive
/// lives in the plugin's data folder instead (see <see cref="Platform.IPluginPaths"/>).
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the path of a yt-dlp executable to use instead of the one TubeDLFin manages.
    /// Blank means TubeDLFin downloads and updates yt-dlp itself.
    /// </summary>
    public string YtDlpPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of a Deno executable to use instead of the one TubeDLFin manages.
    /// Blank means TubeDLFin downloads and updates Deno itself.
    /// </summary>
    public string DenoPath { get; set; } = string.Empty;
}
