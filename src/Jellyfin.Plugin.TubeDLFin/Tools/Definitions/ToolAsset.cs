namespace Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

/// <summary>
/// A release file to download for one platform.
/// </summary>
/// <param name="FileName">The release asset's file name.</param>
/// <param name="ChecksumFileName">The release asset listing the SHA-256 of <paramref name="FileName"/>.</param>
/// <param name="ArchiveEntry">The executable's path inside the zip, or <c>null</c> if the asset is the executable itself.</param>
public sealed record ToolAsset(string FileName, string ChecksumFileName, string? ArchiveEntry = null);
