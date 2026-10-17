using System.Xml.Linq;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;

/// <summary>
/// An <c>.nfo</c> file to write.
/// </summary>
/// <param name="Path">Where to write it.</param>
/// <param name="Document">Its content.</param>
/// <param name="Overwrite">Whether an existing file is replaced (a show's <c>tvshow.nfo</c> is written only once).</param>
public sealed record NfoFile(string Path, XDocument Document, bool Overwrite);
