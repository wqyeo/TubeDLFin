using System;
using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Reads <see cref="IRuntimeEnvironment"/> from the running process.
/// </summary>
public sealed class RuntimeEnvironment : IRuntimeEnvironment
{
    /// <inheritdoc />
    public string OperatingSystemDescription => RuntimeInformation.OSDescription;

    /// <inheritdoc />
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <inheritdoc />
    public bool IsLinux => OperatingSystem.IsLinux();

    /// <inheritdoc />
    public bool IsWindows => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool IsMacOS => OperatingSystem.IsMacOS();
}
