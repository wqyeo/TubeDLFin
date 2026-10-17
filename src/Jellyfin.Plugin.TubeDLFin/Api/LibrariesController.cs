using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Plugin.TubeDLFin.Downloads.Naming;
using Jellyfin.Plugin.TubeDLFin.Libraries;
using Jellyfin.Plugin.TubeDLFin.Platform;
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
    private readonly INamingPresetCatalog _naming;
    private readonly IDiskSpaceProvider _diskSpace;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrariesController"/> class.
    /// </summary>
    /// <param name="libraries">Lists the libraries.</param>
    /// <param name="naming">The naming presets.</param>
    /// <param name="diskSpace">Reports free space.</param>
    public LibrariesController(ILibraryCatalog libraries, INamingPresetCatalog naming, IDiskSpaceProvider diskSpace)
    {
        _libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
        _naming = naming ?? throw new ArgumentNullException(nameof(naming));
        _diskSpace = diskSpace ?? throw new ArgumentNullException(nameof(diskSpace));
    }

    /// <summary>
    /// Gets the video and music libraries with their types, default naming, folders and free space.
    /// </summary>
    /// <response code="200">The libraries, sorted by name.</response>
    /// <returns>The libraries.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<LibraryView>> GetLibraries() =>
        Ok(_libraries.GetLibraries()
            .Select(library => new LibraryView(
                library.Id,
                library.Name,
                library.Type,
                _naming.DefaultFor(library.Type).Id,
                library.Folders.Select(folder => new LibraryFolderView(folder, _diskSpace.GetFreeBytes(folder))).ToList()))
            .ToList());
}
