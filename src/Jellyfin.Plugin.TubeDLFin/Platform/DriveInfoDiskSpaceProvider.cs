using System;
using System.IO;

namespace Jellyfin.Plugin.TubeDLFin.Platform;

/// <summary>
/// <see cref="IDiskSpaceProvider"/> using <see cref="DriveInfo"/>, which on Linux and macOS
/// asks the file system holding the given path.
/// </summary>
public sealed class DriveInfoDiskSpaceProvider : IDiskSpaceProvider
{
    /// <inheritdoc />
    public long? GetFreeBytes(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var directory = Path.GetFullPath(path);
            while (!Directory.Exists(directory))
            {
                directory = Path.GetDirectoryName(directory);
                if (directory is null)
                {
                    return null;
                }
            }

            var drive = new DriveInfo(directory);
            return drive.IsReady ? drive.AvailableFreeSpace : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
