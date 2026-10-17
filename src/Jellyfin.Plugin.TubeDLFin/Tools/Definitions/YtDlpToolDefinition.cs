using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

/// <summary>
/// yt-dlp's standalone builds from <c>github.com/yt-dlp/yt-dlp</c>. All files are listed in one
/// <c>SHA2-256SUMS</c> file per release. Versions look like <c>2026.08.19</c>.
/// </summary>
public sealed class YtDlpToolDefinition : IToolDefinition
{
    private const string ChecksumFile = "SHA2-256SUMS";

    /// <inheritdoc />
    public ToolKind Kind => ToolKind.YtDlp;

    /// <inheritdoc />
    public string DisplayName => "yt-dlp";

    /// <inheritdoc />
    public string Repository => "yt-dlp/yt-dlp";

    /// <inheritdoc />
    public IReadOnlyList<string> VersionArguments { get; } = ["--version"];

    /// <inheritdoc />
    public string GetExecutableName(ToolPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        return platform.ExecutableName("yt-dlp");
    }

    /// <inheritdoc />
    public ToolAsset? GetAsset(ToolPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        var fileName = (platform.Os, platform.Architecture, platform.Libc) switch
        {
            (OsFamily.Linux, Architecture.X64, LibcFlavor.Glibc) => "yt-dlp_linux",
            (OsFamily.Linux, Architecture.Arm64, LibcFlavor.Glibc) => "yt-dlp_linux_aarch64",
            (OsFamily.Linux, Architecture.X64, LibcFlavor.Musl) => "yt-dlp_musllinux",
            (OsFamily.Linux, Architecture.Arm64, LibcFlavor.Musl) => "yt-dlp_musllinux_aarch64",
            (OsFamily.Windows, Architecture.X64, _) => "yt-dlp.exe",
            (OsFamily.Windows, Architecture.Arm64, _) => "yt-dlp_arm64.exe",
            (OsFamily.Windows, Architecture.X86, _) => "yt-dlp_x86.exe",
            (OsFamily.MacOS, Architecture.X64 or Architecture.Arm64, _) => "yt-dlp_macos",
            _ => null,
        };

        return fileName is null ? null : new ToolAsset(fileName, ChecksumFile);
    }

    /// <inheritdoc />
    public string VersionFromTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return tag.Trim();
    }

    /// <inheritdoc />
    public string? ParseVersion(string output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var version = output.Trim().Split('\n', 2)[0].Trim();
        return version.Length == 0 ? null : version;
    }
}
