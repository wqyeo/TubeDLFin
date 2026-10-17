namespace Jellyfin.Plugin.TubeDLFin.Processes;

/// <summary>
/// One line a running program wrote.
/// </summary>
/// <param name="Text">The line, without its line break.</param>
/// <param name="IsError">Whether it was written to standard error.</param>
public sealed record ProcessOutputLine(string Text, bool IsError);
