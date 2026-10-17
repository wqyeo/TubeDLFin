namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// The current state of one tool.
/// </summary>
/// <param name="Kind">The tool.</param>
/// <param name="Name">The tool's display name.</param>
/// <param name="State">Whether it can be used.</param>
/// <param name="Version">The installed version, if it runs.</param>
/// <param name="Path">The executable's path.</param>
/// <param name="IsCustom">Whether the path is a custom one set by the admin.</param>
/// <param name="Message">Details for the admin, e.g. why it's broken.</param>
public sealed record ToolStatus(ToolKind Kind, string Name, ToolState State, string? Version, string Path, bool IsCustom, string? Message);
