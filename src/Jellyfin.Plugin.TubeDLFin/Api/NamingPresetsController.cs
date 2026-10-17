using System;
using System.Collections.Generic;
using System.Net.Mime;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The file naming presets the download form offers. Administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix + "/NamingPresets")]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class NamingPresetsController : ControllerBase
{
    private readonly INamingPresetCatalog _naming;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamingPresetsController"/> class.
    /// </summary>
    /// <param name="naming">The presets.</param>
    public NamingPresetsController(INamingPresetCatalog naming)
    {
        _naming = naming ?? throw new ArgumentNullException(nameof(naming));
    }

    /// <summary>
    /// Gets the naming presets, with the library types each is the default for.
    /// </summary>
    /// <response code="200">The presets, in the order the form lists them.</response>
    /// <returns>The built-in presets.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<NamingPreset>> GetPresets() => Ok(_naming.Presets);
}
