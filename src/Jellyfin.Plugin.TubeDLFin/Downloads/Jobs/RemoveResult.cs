namespace Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;

/// <summary>
/// The outcome of removing a job from the list.
/// </summary>
public enum RemoveResult
{
    /// <summary>The job was removed (its files are kept).</summary>
    Removed,

    /// <summary>There is no such job.</summary>
    NotFound,

    /// <summary>The job is still queued or running; cancel it first.</summary>
    NotFinished,
}
