using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Installation;

/// <summary>
/// Installs one release of a tool.
/// </summary>
public interface IToolInstaller
{
    /// <summary>
    /// Downloads, verifies and installs a release asset. The existing executable is only replaced
    /// once the new one has passed its checksum and run successfully.
    /// </summary>
    /// <param name="definition">The tool.</param>
    /// <param name="asset">The release file for this platform.</param>
    /// <param name="tag">The release tag to install.</param>
    /// <param name="destinationPath">Where the executable must end up.</param>
    /// <param name="cancellationToken">Cancels the installation; the existing executable is left alone.</param>
    /// <returns>The installed version, as reported by the tool itself.</returns>
    /// <exception cref="ToolInstallException">The download was corrupt or the tool doesn't run here.</exception>
    Task<string> InstallAsync(IToolDefinition definition, ToolAsset asset, string tag, string destinationPath, CancellationToken cancellationToken);
}
