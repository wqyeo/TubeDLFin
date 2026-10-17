using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Updates;

/// <summary>
/// Installs missing tools and updates outdated ones. Only one run happens at a time.
/// </summary>
public interface IToolUpdateService
{
    /// <summary>
    /// Gets a value indicating whether an update run is in progress.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the report of the last finished run since the server started, if any.
    /// </summary>
    ToolUpdateReport? LastReport { get; }

    /// <summary>
    /// Starts an update run, or joins the one already in progress.
    /// </summary>
    /// <param name="progress">Receives progress from 0 to 100, if this call starts the run.</param>
    /// <param name="cancellationToken">Cancels the run, if this call starts it.</param>
    /// <returns>The run's report. Failures of single tools are reported, not thrown.</returns>
    Task<ToolUpdateReport> UpdateAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
