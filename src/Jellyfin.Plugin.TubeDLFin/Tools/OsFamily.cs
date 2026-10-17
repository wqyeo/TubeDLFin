namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// Operating system families that tools are built for.
/// </summary>
public enum OsFamily
{
    /// <summary>Linux.</summary>
    Linux,

    /// <summary>Windows.</summary>
    Windows,

    /// <summary>macOS.</summary>
    MacOS,

    /// <summary>Anything else (FreeBSD, ...): no prebuilt tools.</summary>
    Other,
}
