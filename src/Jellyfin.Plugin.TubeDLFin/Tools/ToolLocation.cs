namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// Where a tool's executable is expected.
/// </summary>
/// <param name="Kind">The tool.</param>
/// <param name="ExecutablePath">The executable's full path (it may not exist yet).</param>
/// <param name="IsCustom">Whether the admin set this path; TubeDLFin never updates custom tools.</param>
public sealed record ToolLocation(ToolKind Kind, string ExecutablePath, bool IsCustom);
