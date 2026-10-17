namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// A library folder and the free space on its disk.
/// </summary>
/// <param name="Path">The folder.</param>
/// <param name="FreeBytes">Free space in bytes, if known.</param>
public sealed record LibraryFolderView(string Path, long? FreeBytes);
