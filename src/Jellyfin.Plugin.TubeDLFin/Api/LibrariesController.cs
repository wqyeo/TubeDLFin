using System;
using System.Collections.Generic;
using System.Net.Mime;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// The libraries TubeDLFin can download into. Administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix + "/Libraries")]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class LibrariesController : ControllerBase
{
    private readonly ILibraryCatalog _libraries;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrariesController"/> class.
    /// </summary>
    /// <param name="libraries">Lists the libraries.</param>
    public LibrariesController(ILibraryCatalog libraries)
    {
        _libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
    }

    /// <summary>
    /// Gets the video and music libraries with their types and folders.
    /// </summary>
    /// <response code="200">The libraries, sorted by name.</response>
    /// <returns>The libraries.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<LibraryInfo>> GetLibraries() => Ok(_libraries.GetLibraries());
}
