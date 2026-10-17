using System;

namespace Jellyfin.Plugin.TubeDLFin;

/// <summary>
/// Fixed identifiers of the plugin. Keep in sync with <c>build.yaml</c> (a test checks it).
/// </summary>
public static class PluginIdentity
{
    /// <summary>
    /// The plugin's unique id, as a string.
    /// </summary>
    public const string IdString = "0975c0da-6b11-4771-99fd-7d1176bf1f94";

    /// <summary>
    /// The display name of the plugin.
    /// </summary>
    public const string Name = "TubeDLFin";

    /// <summary>
    /// The route prefix of every API endpoint, e.g. <c>/TubeDLFin/Info</c>.
    /// </summary>
    public const string RoutePrefix = "TubeDLFin";

    /// <summary>
    /// The name of the folder that holds the plugin's data (tools, cache, temporary files).
    /// </summary>
    public const string DataFolderName = "tubedlfin";

    /// <summary>
    /// Gets the plugin's unique id.
    /// </summary>
    public static Guid Id { get; } = Guid.Parse(IdString);
}
