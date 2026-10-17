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
}
