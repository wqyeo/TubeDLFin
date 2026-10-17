using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

/// <summary>
/// The naming presets, and the templates a download uses.
/// </summary>
public interface INamingPresetCatalog
{
    /// <summary>
    /// Gets the built-in presets.
    /// </summary>
    IReadOnlyList<NamingPreset> Presets { get; }

    /// <summary>
    /// Gets the default preset for a library type.
    /// </summary>
    /// <param name="type">The library type.</param>
    /// <returns>The preset.</returns>
    NamingPreset DefaultFor(LibraryType type);

    /// <summary>
    /// Gets the templates a profile uses: its preset's, or its custom template.
    /// </summary>
    /// <param name="profile">The download profile (already validated).</param>
    /// <returns>The media and poster templates.</returns>
    OutputTemplates Resolve(DownloadProfile profile);
}
