namespace Jellyfin.Plugin.TubeDLFin.Tools.Updates;

/// <summary>
/// The update outcome for one tool.
/// </summary>
/// <param name="Kind">The tool.</param>
/// <param name="Name">The tool's display name.</param>
/// <param name="Outcome">What happened.</param>
/// <param name="Version">The version now installed, if known.</param>
/// <param name="Message">Details for the admin, e.g. the error.</param>
public sealed record ToolUpdateResult(ToolKind Kind, string Name, ToolUpdateOutcome Outcome, string? Version, string? Message);
