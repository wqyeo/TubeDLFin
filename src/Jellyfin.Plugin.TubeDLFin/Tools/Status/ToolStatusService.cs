using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Updates;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// <see cref="IToolStatusService"/> combining the locator, the version probe, FFmpeg and the update service.
/// </summary>
public sealed class ToolStatusService : IToolStatusService
{
    private readonly IToolDefinitionRegistry _definitions;
    private readonly IToolLocator _locator;
    private readonly IToolVersionProbe _probe;
    private readonly IFfmpegLocator _ffmpeg;
    private readonly IToolUpdateService _updates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolStatusService"/> class.
    /// </summary>
    /// <param name="definitions">The tools.</param>
    /// <param name="locator">Where each tool lives.</param>
    /// <param name="probe">Reads versions.</param>
    /// <param name="ffmpeg">Finds Jellyfin's FFmpeg.</param>
    /// <param name="updates">The update service, for its progress and last report.</param>
    public ToolStatusService(
        IToolDefinitionRegistry definitions,
        IToolLocator locator,
        IToolVersionProbe probe,
        IFfmpegLocator ffmpeg,
        IToolUpdateService updates)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _ffmpeg = ffmpeg ?? throw new ArgumentNullException(nameof(ffmpeg));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
    }

    /// <inheritdoc />
    public async Task<ToolsOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var tools = new List<ToolStatus>();
        foreach (var definition in _definitions.All)
        {
            tools.Add(await GetStatusAsync(definition, cancellationToken).ConfigureAwait(false));
        }

        return new ToolsOverview(_locator.Platform.Id, tools, _ffmpeg.GetStatus(), _updates.IsRunning, _updates.LastReport);
    }

    /// <inheritdoc />
    public Task<ToolStatus> GetStatusAsync(ToolKind kind, CancellationToken cancellationToken) =>
        GetStatusAsync(_definitions.GetDefinition(kind), cancellationToken);

    private static ToolStatus Status(IToolDefinition definition, ToolLocation location, ToolState state, string? version, string? message) =>
        new(definition.Kind, definition.DisplayName, state, version, location.ExecutablePath, location.IsCustom, message);

    private async Task<ToolStatus> GetStatusAsync(IToolDefinition definition, CancellationToken cancellationToken)
    {
        var location = _locator.Locate(definition.Kind);

        if (!location.IsCustom && !File.Exists(location.ExecutablePath))
        {
            return definition.GetAsset(_locator.Platform) is null
                ? Status(definition, location, ToolState.Unsupported, null, _locator.Platform.UnsupportedMessage(definition.DisplayName))
                : Status(definition, location, ToolState.NotInstalled, null, null);
        }

        var probe = await _probe.ProbeAsync(definition, location.ExecutablePath, cancellationToken).ConfigureAwait(false);
        return probe.Works
            ? Status(definition, location, ToolState.Ready, probe.Version, null)
            : Status(definition, location, ToolState.Broken, null, probe.Error);
    }
}
