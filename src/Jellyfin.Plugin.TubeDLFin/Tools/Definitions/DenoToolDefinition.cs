using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

/// <summary>
/// Deno's release zips from <c>github.com/denoland/deno</c>, each with its own
/// <c>.sha256sum</c> file. Tags look like <c>v2.9.7</c>. There are no musl builds.
/// </summary>
public sealed class DenoToolDefinition : IToolDefinition
{
    /// <inheritdoc />
    public ToolKind Kind => ToolKind.Deno;

    /// <inheritdoc />
    public string DisplayName => "Deno";

    /// <inheritdoc />
    public string Repository => "denoland/deno";

    /// <inheritdoc />
    public IReadOnlyList<string> VersionArguments { get; } = ["--version"];

    /// <inheritdoc />
    public string GetExecutableName(ToolPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        return platform.ExecutableName("deno");
    }

    /// <inheritdoc />
    public ToolAsset? GetAsset(ToolPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        var cpu = platform.Architecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "aarch64",
            _ => null,
        };

        var system = (platform.Os, platform.Libc) switch
        {
            (OsFamily.Linux, LibcFlavor.Glibc) => "unknown-linux-gnu",
            (OsFamily.Windows, _) => "pc-windows-msvc",
            (OsFamily.MacOS, _) => "apple-darwin",
            _ => null,
        };

        if (cpu is null || system is null)
        {
            return null;
        }

        var zip = $"deno-{cpu}-{system}.zip";
        return new ToolAsset(zip, zip + ".sha256sum", GetExecutableName(platform));
    }

    /// <inheritdoc />
    public string VersionFromTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return tag.Trim().TrimStart('v');
    }

    /// <inheritdoc />
    public string? ParseVersion(string output)
    {
        ArgumentNullException.ThrowIfNull(output);

        // First line: "deno 2.9.7 (stable, release, x86_64-unknown-linux-gnu)"
        var words = output.Trim().Split('\n', 2)[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 2 && string.Equals(words[0], "deno", StringComparison.OrdinalIgnoreCase) ? words[1] : null;
    }
}
