using Jellyfin.Plugin.TubeDLFin.Detection;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The HTTP status for a link yt-dlp couldn't list, shared by the endpoints that list links.
/// </summary>
internal static class ProbeFailureStatus
{
    /// <summary>
    /// nginx's "client closed request": nobody is left to read the response.
    /// </summary>
    public const int ClientClosedRequest = 499;

    /// <summary>
    /// Gets the status code for a failure.
    /// </summary>
    /// <param name="reason">Why listing failed.</param>
    /// <returns>504 for a timeout, 502 for unreadable output, otherwise 422.</returns>
    public static int For(ProbeFailure reason) => reason switch
    {
        ProbeFailure.TimedOut => StatusCodes.Status504GatewayTimeout,
        ProbeFailure.UnreadableOutput => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status422UnprocessableEntity,
    };
}
