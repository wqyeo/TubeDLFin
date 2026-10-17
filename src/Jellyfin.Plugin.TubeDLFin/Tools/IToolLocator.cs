namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// Decides which executable to use for each tool: the admin's custom path if set,
/// otherwise the copy TubeDLFin manages.
/// </summary>
public interface IToolLocator
{
    /// <summary>
    /// Gets the platform tool binaries must be built for.
    /// </summary>
    ToolPlatform Platform { get; }

    /// <summary>
    /// Finds where a tool's executable is expected.
    /// </summary>
    /// <param name="kind">The tool.</param>
    /// <returns>The location.</returns>
    ToolLocation Locate(ToolKind kind);
}
