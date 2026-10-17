using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The yt-dlp / Deno / FFmpeg status and the "update now" action. Administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix + "/Tools")]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class ToolsController : ControllerBase
{
    private readonly IToolStatusService _status;
    private readonly IToolUpdateService _updates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolsController"/> class.
    /// </summary>
    /// <param name="status">Reports the tools' state.</param>
    /// <param name="updates">Installs and updates tools.</param>
    public ToolsController(IToolStatusService status, IToolUpdateService updates)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
    }

    /// <summary>
    /// Gets the state of yt-dlp, Deno and Jellyfin's FFmpeg.
    /// </summary>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="200">The current state of every tool.</response>
    /// <returns>The overview.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ToolsOverview>> GetTools(CancellationToken cancellationToken) =>
        await _status.GetOverviewAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Installs missing tools and updates outdated ones now, then returns the new state.
    /// </summary>
    /// <param name="cancellationToken">The request's cancellation token. Closing the page stops the waiting, not the update.</param>
    /// <response code="200">The overview after the update; per-tool results are in <c>LastUpdate</c>.</response>
    /// <returns>The overview.</returns>
    [HttpPost("Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ToolsOverview>> UpdateTools(CancellationToken cancellationToken)
    {
        await _updates.UpdateAsync(progress: null, CancellationToken.None).WaitAsync(cancellationToken).ConfigureAwait(false);
        return await _status.GetOverviewAsync(cancellationToken).ConfigureAwait(false);
    }
}
