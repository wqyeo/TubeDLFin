using System;
using System.Net.Mime;
using Jellyfin.Plugin.TubeDLFin.Status;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The plugin's HTTP API. Every endpoint is for administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix)]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class TubeDLFinController : ControllerBase
{
    private readonly IPluginInfoProvider _infoProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TubeDLFinController"/> class.
    /// </summary>
    /// <param name="infoProvider">Provides the plugin and server facts.</param>
    public TubeDLFinController(IPluginInfoProvider infoProvider)
    {
        _infoProvider = infoProvider ?? throw new ArgumentNullException(nameof(infoProvider));
    }

    /// <summary>
    /// Gets basic facts about the plugin and the server.
    /// </summary>
    /// <response code="200">The facts.</response>
    /// <returns>The plugin and server facts.</returns>
    [HttpGet("Info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<TubeDLFinInfo> GetInfo() => _infoProvider.GetInfo();
}
