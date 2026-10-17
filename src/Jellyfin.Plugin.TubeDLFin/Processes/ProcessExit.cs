namespace Jellyfin.Plugin.TubeDLFin.Processes;

/// <summary>
/// How a streamed program ended.
/// </summary>
/// <param name="ExitCode">The exit code (meaningless if <paramref name="TimedOut"/> is set).</param>
/// <param name="TimedOut">Whether the program was killed for running past its timeout.</param>
public sealed record ProcessExit(int ExitCode, bool TimedOut)
{
    /// <summary>
    /// Gets a value indicating whether the program finished in time with exit code 0.
    /// </summary>
    public bool Succeeded => !TimedOut && ExitCode == 0;
}
