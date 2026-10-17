using System;
using System.IO;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

/// <summary>
/// A unique folder under the system temp folder, deleted on dispose.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tubedlfin-tests", Guid.NewGuid().ToString("N"))).FullName;
    }

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public string WriteFile(string relativePath, string content)
    {
        var path = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
