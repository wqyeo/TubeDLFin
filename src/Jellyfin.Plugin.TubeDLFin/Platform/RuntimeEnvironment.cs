using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// Reads <see cref="IRuntimeEnvironment"/> from the running process.
/// </summary>
public sealed class RuntimeEnvironment : IRuntimeEnvironment
{
    private readonly Lazy<bool> _isMusl = new(DetectMusl);

    /// <inheritdoc />
    public string OperatingSystemDescription => RuntimeInformation.OSDescription;

    /// <inheritdoc />
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <inheritdoc />
    public bool IsLinux => OperatingSystem.IsLinux();

    /// <inheritdoc />
    public bool IsMusl => _isMusl.Value;

    /// <inheritdoc />
    public bool IsWindows => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool IsMacOS => OperatingSystem.IsMacOS();

    /// <summary>
    /// musl systems ship their dynamic loader as <c>/lib/ld-musl-&lt;arch&gt;.so.1</c>; glibc systems don't have it.
    /// </summary>
    private static bool DetectMusl()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return Directory.EnumerateFiles("/lib", "ld-musl-*").Any();
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
