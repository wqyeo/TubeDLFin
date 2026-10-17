namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// Whether a tool can be used.
/// </summary>
public enum ToolState
{
    /// <summary>The tool runs and reported its version.</summary>
    Ready,

    /// <summary>The managed tool hasn't been downloaded yet.</summary>
    NotInstalled,

    /// <summary>The executable exists but doesn't run; see the message.</summary>
    Broken,

    /// <summary>No prebuilt binary exists for this platform and no custom path is set.</summary>
    Unsupported,
}
