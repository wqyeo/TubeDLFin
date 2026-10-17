using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Where files go and what they are called. Partial files stay in the job's temp folder;
/// yt-dlp moves each finished file into the library, so Jellyfin never sees half-written files.
/// </summary>
public sealed class OutputOptions : IDownloadOptionSet
{
    /// <summary>
    /// The file name template: <c>Channel/Title [id].ext</c>.
    /// </summary>
    public const string Template = "%(uploader,channel,creator|Unknown)s/%(title)s [%(id)s].%(ext)s";

    // Long titles in UTF-8 can pass the 255-byte file name limit; keep room for " [id].ext".
    private const string MaxFileNameLength = "150";

    /// <inheritdoc />
    public void AddTo(IList<string> arguments, DownloadPlan plan, DownloadLocations locations)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(locations);

        arguments.Add("--paths");
        arguments.Add(plan.Destination);
        arguments.Add("--paths");
        arguments.Add("temp:" + locations.TempDirectory);
        arguments.Add("--output");
        arguments.Add(Template);

        // "Recently added" should sort by download time, not by the upload date.
        arguments.Add("--no-mtime");

        // Libraries often live on SMB/NAS shares: avoid characters Windows can't store.
        arguments.Add("--windows-filenames");
        arguments.Add("--trim-filenames");
        arguments.Add(MaxFileNameLength);
    }
}
