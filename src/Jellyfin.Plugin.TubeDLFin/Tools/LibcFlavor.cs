namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// The C library a Linux system uses. Prebuilt binaries for one don't run on the other.
/// </summary>
public enum LibcFlavor
{
    /// <summary>Not Linux, so not applicable.</summary>
    None,

    /// <summary>glibc (Debian, Ubuntu, Fedora; the official Jellyfin image).</summary>
    Glibc,

    /// <summary>musl (Alpine).</summary>
    Musl,
}
