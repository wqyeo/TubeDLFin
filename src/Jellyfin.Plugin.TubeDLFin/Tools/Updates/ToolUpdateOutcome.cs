namespace Jellyfin.Plugin.TubeDLFin.Tools.Updates;

/// <summary>
/// What happened to one tool during an update run.
/// </summary>
public enum ToolUpdateOutcome
{
    /// <summary>The tool was missing (or broken) and has been installed.</summary>
    Installed,

    /// <summary>A newer release replaced the installed one.</summary>
    Updated,

    /// <summary>The installed release is the newest.</summary>
    UpToDate,

    /// <summary>The admin set a custom path, so TubeDLFin leaves the tool alone.</summary>
    SkippedCustom,

    /// <summary>No prebuilt binary exists for this platform.</summary>
    Unsupported,

    /// <summary>Something went wrong; see the message.</summary>
    Failed,
}
