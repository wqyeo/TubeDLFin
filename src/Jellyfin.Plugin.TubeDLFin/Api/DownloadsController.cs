using System;
using System.Collections.Generic;
using System.Net.Mime;
using Jellyfin.Plugin.TubeDLFin.Downloads;
using Jellyfin.Plugin.TubeDLFin.Downloads.Jobs;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The download queue: add, list, cancel and remove downloads. Administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix + "/Downloads")]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class DownloadsController : ControllerBase
{
    private readonly IDownloadManager _downloads;
    private readonly IDownloadRequestValidator _validator;
    private readonly ILibraryCatalog _libraries;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadsController"/> class.
    /// </summary>
    /// <param name="downloads">The download queue.</param>
    /// <param name="validator">Checks new downloads.</param>
    /// <param name="libraries">The libraries that can be downloaded into.</param>
    public DownloadsController(IDownloadManager downloads, IDownloadRequestValidator validator, ILibraryCatalog libraries)
    {
        _downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
    }

    /// <summary>
    /// Gets every download, newest first.
    /// </summary>
    /// <response code="200">The downloads.</response>
    /// <returns>Snapshots of every job.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DownloadJobInfo>> GetDownloads() => Ok(_downloads.GetJobs());

    /// <summary>
    /// Adds a download to the queue.
    /// </summary>
    /// <param name="request">The link, options and destination.</param>
    /// <response code="202">The download was queued.</response>
    /// <response code="400">Something in the request is invalid; the reasons are in <c>detail</c>.</response>
    /// <returns>The new job.</returns>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DownloadJobInfo> AddDownload([FromBody] DownloadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = _validator.Validate(request, _libraries.GetLibraries());
        if (!validation.IsValid)
        {
            return Problem(string.Join(" ", validation.Errors), statusCode: StatusCodes.Status400BadRequest);
        }

        return Accepted(_downloads.Enqueue(validation.Plan!, DownloadSources.Manual));
    }

    /// <summary>
    /// Gets a download's last log lines.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <response code="200">The lines, oldest first.</response>
    /// <response code="404">There is no such download.</response>
    /// <returns>The log.</returns>
    [HttpGet("{id}/Log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<string>> GetLog([FromRoute] Guid id) =>
        _downloads.GetLog(id) is { } log ? Ok(log) : NotFound();

    /// <summary>
    /// Cancels a queued or running download and stops yt-dlp. Files already finished are kept.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <response code="200">The job after cancelling.</response>
    /// <response code="404">There is no such download.</response>
    /// <returns>The job.</returns>
    [HttpPost("{id}/Cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DownloadJobInfo> Cancel([FromRoute] Guid id) =>
        _downloads.Cancel(id) is { } job ? Ok(job) : NotFound();

    /// <summary>
    /// Removes a finished download from the list. Its files are kept.
    /// </summary>
    /// <param name="id">The job's id.</param>
    /// <response code="204">Removed.</response>
    /// <response code="404">There is no such download.</response>
    /// <response code="409">The download hasn't ended; cancel it first.</response>
    /// <returns>Nothing.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult Remove([FromRoute] Guid id) => _downloads.Remove(id) switch
    {
        RemoveResult.Removed => NoContent(),
        RemoveResult.NotFinished => Problem("The download is still running. Cancel it first.", statusCode: StatusCodes.Status409Conflict),
        _ => NotFound(),
    };

    /// <summary>
    /// Removes every finished download from the list. Their files are kept.
    /// </summary>
    /// <response code="200">How many were removed.</response>
    /// <returns>The count.</returns>
    [HttpPost("ClearFinished")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ClearFinishedResult> ClearFinished() => Ok(new ClearFinishedResult(_downloads.ClearFinished()));
}
