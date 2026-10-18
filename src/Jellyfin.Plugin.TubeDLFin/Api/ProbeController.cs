using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// Detects what a link points to. Administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix)]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class ProbeController : ControllerBase
{
    private readonly IMediaProber _prober;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeController"/> class.
    /// </summary>
    /// <param name="prober">Detects links.</param>
    public ProbeController(IMediaProber prober)
    {
        _prober = prober ?? throw new ArgumentNullException(nameof(prober));
    }

    /// <summary>
    /// Detects a video, playlist or channel: its details, formats, subtitles or items.
    /// </summary>
    /// <param name="request">The link.</param>
    /// <param name="cancellationToken">The request's cancellation token. Closing the page stops yt-dlp.</param>
    /// <response code="200">What was found.</response>
    /// <response code="400">The link is not a valid http(s) link.</response>
    /// <response code="422">yt-dlp refused the link; the reason is in <c>detail</c>.</response>
    /// <response code="502">yt-dlp's output could not be read.</response>
    /// <response code="503">yt-dlp is not installed or not working.</response>
    /// <response code="504">yt-dlp did not finish in time.</response>
    /// <returns>The detection result.</returns>
    [HttpPost("Probe")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ProbeResult>> Probe([FromBody] ProbeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MediaUrl.TryCreate(request.Url, out var url, out var error))
        {
            return Problem(error, statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            return await _prober.ProbeAsync(url, request.NoPlaylist, IMediaProber.DetectEntryLimit, cancellationToken).ConfigureAwait(false);
        }
        catch (ToolNotReadyException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ProbeFailedException ex)
        {
            return Problem(ex.Message, statusCode: ProbeFailureStatus.For(ex.Reason));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(ProbeFailureStatus.ClientClosedRequest);
        }
    }
}
