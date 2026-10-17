using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// Runs a tool's executable to check that it works and to read its version.
/// </summary>
public interface IToolVersionProbe
{
    /// <summary>
    /// Runs the executable with the definition's version arguments.
    /// </summary>
    /// <param name="definition">The tool.</param>
    /// <param name="executablePath">The executable to run.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The version or the reason it failed. Never throws for a broken tool.</returns>
    Task<ToolProbeResult> ProbeAsync(IToolDefinition definition, string executablePath, CancellationToken cancellationToken);
}
