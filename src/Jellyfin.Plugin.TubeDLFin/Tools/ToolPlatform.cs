using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Jellyfin.Plugin.TubeDLFin.Platform;

namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// The platform a tool binary has to be built for.
/// </summary>
/// <param name="Os">The operating system family.</param>
/// <param name="Architecture">The CPU architecture.</param>
/// <param name="Libc">The C library (Linux only).</param>
public sealed record ToolPlatform(OsFamily Os, Architecture Architecture, LibcFlavor Libc)
{
    /// <summary>
    /// Gets a short readable id such as <c>linux-arm64-glibc</c> or <c>windows-x64</c>.
    /// </summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "A display id, never compared.")]
    public string Id => Libc == LibcFlavor.None
        ? $"{Os}-{Architecture}".ToLowerInvariant()
        : $"{Os}-{Architecture}-{Libc}".ToLowerInvariant();

    /// <summary>
    /// Gets a value indicating whether executables need a <c>.exe</c> suffix.
    /// </summary>
    public bool IsWindows => Os == OsFamily.Windows;

    /// <summary>
    /// Works out the platform of the running server.
    /// </summary>
    /// <param name="environment">Facts about the running server.</param>
    /// <returns>The platform.</returns>
    public static ToolPlatform FromEnvironment(IRuntimeEnvironment environment)
    {
        System.ArgumentNullException.ThrowIfNull(environment);

        if (environment.IsLinux)
        {
            var libc = environment.IsMusl ? LibcFlavor.Musl : LibcFlavor.Glibc;
            return new ToolPlatform(OsFamily.Linux, environment.ProcessArchitecture, libc);
        }

        var os = environment.IsWindows ? OsFamily.Windows
            : environment.IsMacOS ? OsFamily.MacOS
            : OsFamily.Other;
        return new ToolPlatform(os, environment.ProcessArchitecture, LibcFlavor.None);
    }

    /// <summary>
    /// Adds <c>.exe</c> on Windows.
    /// </summary>
    /// <param name="baseName">The executable name without extension.</param>
    /// <returns>The file name for this platform.</returns>
    public string ExecutableName(string baseName) => IsWindows ? baseName + ".exe" : baseName;

    /// <summary>
    /// The message shown when a tool has no prebuilt binary for this platform.
    /// </summary>
    /// <param name="toolName">The tool's display name.</param>
    /// <returns>The message.</returns>
    public string UnsupportedMessage(string toolName) =>
        $"No prebuilt {toolName} exists for {Id}. Install it yourself and set its path in the settings.";
}
