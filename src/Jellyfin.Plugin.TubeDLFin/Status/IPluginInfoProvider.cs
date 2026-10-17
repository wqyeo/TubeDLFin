namespace Jellyfin.Plugin.TubeDLFin.Status;

/// <summary>
/// Provides <see cref="TubeDLFinInfo"/>.
/// </summary>
public interface IPluginInfoProvider
{
    /// <summary>
    /// Gets the current plugin and server facts.
    /// </summary>
    /// <returns>The facts.</returns>
    TubeDLFinInfo GetInfo();
}
