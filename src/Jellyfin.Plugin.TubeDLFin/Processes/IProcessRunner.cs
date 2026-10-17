using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Processes;

/// <summary>
/// Starts external programs (yt-dlp, Deno) and collects their output.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a program to completion. On cancellation or timeout the whole process tree is killed.
    /// </summary>
    /// <param name="request">What to run.</param>
    /// <param name="cancellationToken">Kills the program when cancelled.</param>
    /// <returns>The exit code and captured output.</returns>
    /// <exception cref="Win32Exception">The program could not be started (missing, not executable).</exception>
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a program to completion, handing over each output line as soon as it is written.
    /// On cancellation or timeout the whole process tree is killed.
    /// </summary>
    /// <param name="request">What to run.</param>
    /// <param name="onLine">Called for every line of standard output and standard error. Lines
    /// from the two streams can arrive at the same time on different threads.</param>
    /// <param name="cancellationToken">Kills the program when cancelled.</param>
    /// <returns>How the program ended.</returns>
    /// <exception cref="Win32Exception">The program could not be started (missing, not executable).</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled; the program has been killed and every line handed over.</exception>
    Task<ProcessExit> StreamAsync(ProcessRequest request, Action<ProcessOutputLine> onLine, CancellationToken cancellationToken);
}
