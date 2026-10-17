namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Where one download job works.
/// </summary>
/// <param name="TempDirectory">The job's own folder for partial files, outside every library.</param>
/// <param name="ArchiveFile">The library's download archive, or <c>null</c> to download everything again.</param>
public sealed record DownloadLocations(string TempDirectory, string? ArchiveFile);
