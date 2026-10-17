using System.Runtime.InteropServices;
using Jellyfin.Plugin.TubeDLFin.Tools;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

internal static class Platforms
{
    public static readonly ToolPlatform LinuxX64 = new(OsFamily.Linux, Architecture.X64, LibcFlavor.Glibc);
    public static readonly ToolPlatform LinuxArm64 = new(OsFamily.Linux, Architecture.Arm64, LibcFlavor.Glibc);
    public static readonly ToolPlatform LinuxMuslX64 = new(OsFamily.Linux, Architecture.X64, LibcFlavor.Musl);
    public static readonly ToolPlatform LinuxMuslArm64 = new(OsFamily.Linux, Architecture.Arm64, LibcFlavor.Musl);
    public static readonly ToolPlatform LinuxArmV7 = new(OsFamily.Linux, Architecture.Arm, LibcFlavor.Glibc);
    public static readonly ToolPlatform WindowsX64 = new(OsFamily.Windows, Architecture.X64, LibcFlavor.None);
    public static readonly ToolPlatform WindowsArm64 = new(OsFamily.Windows, Architecture.Arm64, LibcFlavor.None);
    public static readonly ToolPlatform WindowsX86 = new(OsFamily.Windows, Architecture.X86, LibcFlavor.None);
    public static readonly ToolPlatform MacX64 = new(OsFamily.MacOS, Architecture.X64, LibcFlavor.None);
    public static readonly ToolPlatform MacArm64 = new(OsFamily.MacOS, Architecture.Arm64, LibcFlavor.None);
    public static readonly ToolPlatform FreeBsdX64 = new(OsFamily.Other, Architecture.X64, LibcFlavor.None);
}
