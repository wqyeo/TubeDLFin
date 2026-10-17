using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// Reports the state of every tool.
/// </summary>
public interface IToolStatusService
{
    /// <summary>
    /// Checks every tool and Jellyfin's FFmpeg.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The overview.</returns>
    Task<ToolsOverview> GetOverviewAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks one tool.
    /// </summary>
    /// <param name="kind">The tool.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The tool's status.</returns>
    Task<ToolStatus> GetStatusAsync(ToolKind kind, CancellationToken cancellationToken);
}
