namespace Jellyfin.Plugin.TubeDLFin.Status;

/// <summary>
/// Basic facts about the plugin and the server it runs on, returned by <c>GET /TubeDLFin/Info</c>.
/// </summary>
public sealed record TubeDLFinInfo
{
    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public required string PluginVersion { get; init; }

    /// <summary>
    /// Gets the Jellyfin server version.
    /// </summary>
    public required string ServerVersion { get; init; }

    /// <summary>
    /// Gets a readable description of the server's operating system.
    /// </summary>
    public required string OperatingSystem { get; init; }

    /// <summary>
    /// Gets the CPU architecture of the server process, e.g. "Arm64" or "X64".
    /// </summary>
    public required string Architecture { get; init; }

    /// <summary>
    /// Gets the folder where the plugin keeps its data.
    /// </summary>
    public required string DataPath { get; init; }
}
