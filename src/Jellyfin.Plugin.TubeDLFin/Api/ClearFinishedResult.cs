namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The response of <c>POST /TubeDLFin/Downloads/ClearFinished</c>.
/// </summary>
/// <param name="Removed">How many finished jobs were removed from the list.</param>
public sealed record ClearFinishedResult(int Removed);
