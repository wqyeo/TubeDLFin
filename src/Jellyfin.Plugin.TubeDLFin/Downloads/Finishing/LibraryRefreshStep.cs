using System;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;
using Jellyfin.Plugin.TubeDLFin.Libraries;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;

/// <summary>
/// Tells Jellyfin about the file, after every other step has written its files.
/// </summary>
public sealed class LibraryRefreshStep : IFinishedItemStep
{
    private readonly ILibraryNotifier _notifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryRefreshStep"/> class.
    /// </summary>
    /// <param name="notifier">Tells Jellyfin about new files.</param>
    public LibraryRefreshStep(ILibraryNotifier notifier)
    {
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
    }

    /// <inheritdoc />
    public void Run(DownloadPlan plan, DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(item);

        _notifier.FileAdded(plan.Library, item.FilePath);
    }
}
