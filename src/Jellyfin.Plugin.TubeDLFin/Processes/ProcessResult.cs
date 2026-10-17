namespace Jellyfin.Plugin.TubeDLFin.Processes;

/// <summary>
/// The outcome of a finished program.
/// </summary>
/// <param name="ExitCode">The exit code (meaningless if <paramref name="TimedOut"/> is set).</param>
/// <param name="StandardOutput">Everything written to standard output.</param>
/// <param name="StandardError">Everything written to standard error.</param>
/// <param name="TimedOut">Whether the program was killed for running past its timeout.</param>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut)
{
    /// <summary>
    /// Gets a value indicating whether the program finished in time with exit code 0.
    /// </summary>
    public bool Succeeded => !TimedOut && ExitCode == 0;
}
