namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// The outcome of running a tool to read its version.
/// </summary>
/// <param name="Version">The version, if the tool ran and printed one.</param>
/// <param name="Error">Why the tool couldn't be used, if it failed.</param>
public sealed record ToolProbeResult(string? Version, string? Error)
{
    /// <summary>
    /// Gets a value indicating whether the tool ran and reported a version.
    /// </summary>
    public bool Works => Version is not null;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The result.</returns>
    public static ToolProbeResult Ok(string version) => new(version, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">What went wrong.</param>
    /// <returns>The result.</returns>
    public static ToolProbeResult Failed(string error) => new(null, error);
}
