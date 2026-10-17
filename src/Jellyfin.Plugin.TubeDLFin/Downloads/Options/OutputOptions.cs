using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Options;

/// <summary>
/// Where files go and what they are called. Partial files stay in the job's temp folder;
/// yt-dlp moves each finished file into the library, so Jellyfin never sees half-written files.
/// </summary>
public sealed class OutputOptions : IDownloadOptionSet
{
    private readonly INamingPresetCatalog _naming;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputOptions"/> class.
    /// </summary>
    /// <param name="naming">Turns the profile's naming choice into templates.</param>
    public OutputOptions(INamingPresetCatalog naming)
    {
        _naming = naming ?? throw new ArgumentNullException(nameof(naming));
    }

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
        var templates = _naming.Resolve(plan.Profile);
        arguments.Add("--output");
        arguments.Add(templates.Media);
        arguments.Add("--output");
        arguments.Add("thumbnail:" + templates.Thumbnail);

        // "Recently added" should sort by download time, not by the upload date.
        arguments.Add("--no-mtime");

        // Libraries often live on SMB/NAS shares: avoid characters Windows can't store. Name
        // lengths are limited by the templates (see NamingPresets).
        arguments.Add("--windows-filenames");
    }
}
