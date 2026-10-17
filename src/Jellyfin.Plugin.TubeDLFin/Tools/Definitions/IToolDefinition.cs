using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

/// <summary>
/// Everything that differs between managed tools: where releases come from, which file fits
/// which platform, and how to read the version. Add a tool by adding an implementation.
/// </summary>
public interface IToolDefinition
{
    /// <summary>
    /// Gets which tool this is.
    /// </summary>
    ToolKind Kind { get; }

    /// <summary>
    /// Gets the name shown to users.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the GitHub repository releases are taken from, as <c>owner/name</c>.
    /// </summary>
    string Repository { get; }

    /// <summary>
    /// Gets the arguments that make the tool print its version.
    /// </summary>
    IReadOnlyList<string> VersionArguments { get; }

    /// <summary>
    /// Gets the executable's file name on a platform, e.g. <c>yt-dlp</c> or <c>yt-dlp.exe</c>.
    /// </summary>
    /// <param name="platform">The platform.</param>
    /// <returns>The file name.</returns>
    string GetExecutableName(ToolPlatform platform);

    /// <summary>
    /// Picks the release file for a platform.
    /// </summary>
    /// <param name="platform">The platform.</param>
    /// <returns>The asset, or <c>null</c> if no prebuilt binary exists for the platform.</returns>
    ToolAsset? GetAsset(ToolPlatform platform);

    /// <summary>
    /// Turns a release tag into a version comparable with <see cref="ParseVersion"/>.
    /// </summary>
    /// <param name="tag">The release tag, e.g. <c>v2.9.7</c>.</param>
    /// <returns>The version, e.g. <c>2.9.7</c>.</returns>
    string VersionFromTag(string tag);

    /// <summary>
    /// Reads the version from the output of <see cref="VersionArguments"/>.
    /// </summary>
    /// <param name="output">The tool's standard output.</param>
    /// <returns>The version, or <c>null</c> if the output isn't recognised.</returns>
    string? ParseVersion(string output);
}
