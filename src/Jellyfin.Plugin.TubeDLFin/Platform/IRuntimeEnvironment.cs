using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Facts about the machine the server runs on. Kept behind an interface so code that picks
/// platform-specific tool builds can be tested for every platform.
/// </summary>
public interface IRuntimeEnvironment
{
    /// <summary>
    /// Gets a readable description of the operating system, e.g. "Debian GNU/Linux 13".
    /// </summary>
    string OperatingSystemDescription { get; }

    /// <summary>
    /// Gets the CPU architecture of the server process.
    /// </summary>
    Architecture ProcessArchitecture { get; }

    /// <summary>
    /// Gets a value indicating whether the operating system is Linux.
    /// </summary>
    bool IsLinux { get; }

    /// <summary>
    /// Gets a value indicating whether the C library is musl (e.g. Alpine) rather than glibc.
    /// Only meaningful on Linux; many prebuilt tools exist for glibc only.
    /// </summary>
    bool IsMusl { get; }

    /// <summary>
    /// Gets a value indicating whether the operating system is Windows.
    /// </summary>
    bool IsWindows { get; }

    /// <summary>
    /// Gets a value indicating whether the operating system is macOS.
    /// </summary>
    bool IsMacOS { get; }
}
