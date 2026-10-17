using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Installation;
using Jellyfin.Plugin.TubeDLFin.Tools.Releases;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Updates;

/// <summary>
/// <see cref="IToolUpdateService"/> that compares each managed tool with its latest release and
/// installs it through <see cref="IToolInstaller"/> when they differ.
/// </summary>
public sealed partial class ToolUpdateService : IToolUpdateService
{
    private readonly IToolDefinitionRegistry _definitions;
    private readonly IToolLocator _locator;
    private readonly IReleaseClient _releases;
    private readonly IToolVersionProbe _probe;
    private readonly IToolInstaller _installer;
    private readonly TimeProvider _time;
    private readonly ILogger<ToolUpdateService> _logger;
    private readonly Lock _gate = new();
    private Task<ToolUpdateReport>? _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolUpdateService"/> class.
    /// </summary>
    /// <param name="definitions">The tools to manage.</param>
    /// <param name="locator">Where each tool lives.</param>
    /// <param name="releases">Finds the latest releases.</param>
    /// <param name="probe">Reads installed versions.</param>
    /// <param name="installer">Installs releases.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    public ToolUpdateService(
        IToolDefinitionRegistry definitions,
        IToolLocator locator,
        IReleaseClient releases,
        IToolVersionProbe probe,
        IToolInstaller installer,
        TimeProvider time,
        ILogger<ToolUpdateService> logger)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _releases = releases ?? throw new ArgumentNullException(nameof(releases));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _current is { IsCompleted: false };
            }
        }
    }

    /// <inheritdoc />
    public ToolUpdateReport? LastReport { get; private set; }

    /// <inheritdoc />
    public Task<ToolUpdateReport> UpdateAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_current is not { IsCompleted: false })
            {
                _current = RunAsync(progress, cancellationToken);
            }

            return _current;
        }
    }

    private static ToolUpdateResult Result(IToolDefinition definition, ToolUpdateOutcome outcome, string? version, string? message) =>
        new(definition.Kind, definition.DisplayName, outcome, version, message);

    private async Task<ToolUpdateReport> RunAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // Leave the caller's lock before doing any work.
        await Task.Yield();

        var results = new List<ToolUpdateResult>();
        var definitions = _definitions.All;
        for (var i = 0; i < definitions.Count; i++)
        {
            results.Add(await UpdateOneAsync(definitions[i], cancellationToken).ConfigureAwait(false));
            progress?.Report(100.0 * (i + 1) / definitions.Count);
        }

        var report = new ToolUpdateReport(_time.GetUtcNow(), results);
        LastReport = report;
        return report;
    }

    private async Task<ToolUpdateResult> UpdateOneAsync(IToolDefinition definition, CancellationToken cancellationToken)
    {
        var location = _locator.Locate(definition.Kind);
        if (location.IsCustom)
        {
            return Result(definition, ToolUpdateOutcome.SkippedCustom, null, $"Using the custom path {location.ExecutablePath}.");
        }

        var asset = definition.GetAsset(_locator.Platform);
        if (asset is null)
        {
            return Result(definition, ToolUpdateOutcome.Unsupported, null, _locator.Platform.UnsupportedMessage(definition.DisplayName));
        }

        try
        {
            var tag = await _releases.GetLatestTagAsync(definition.Repository, cancellationToken).ConfigureAwait(false);
            var latest = definition.VersionFromTag(tag);

            var installed = File.Exists(location.ExecutablePath)
                ? await _probe.ProbeAsync(definition, location.ExecutablePath, cancellationToken).ConfigureAwait(false)
                : null;

            if (installed is { Works: true } && string.Equals(installed.Version, latest, StringComparison.Ordinal))
            {
                return Result(definition, ToolUpdateOutcome.UpToDate, installed.Version, null);
            }

            LogInstalling(definition.DisplayName, tag, installed?.Version ?? "none");
            var version = await _installer.InstallAsync(definition, asset, tag, location.ExecutablePath, cancellationToken).ConfigureAwait(false);
            LogInstalled(definition.DisplayName, version);

            return installed is { Works: true }
                ? Result(definition, ToolUpdateOutcome.Updated, version, $"Updated from {installed.Version}.")
                : Result(definition, ToolUpdateOutcome.Installed, version, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // One tool failing must not stop the others; the error is reported to the admin.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogFailed(ex, definition.DisplayName);
            return Result(definition, ToolUpdateOutcome.Failed, null, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Installing {Tool} {Tag} (installed: {Installed})")]
    private partial void LogInstalling(string tool, string tag, string installed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Installed {Tool} {Version}")]
    private partial void LogInstalled(string tool, string version);

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating {Tool} failed")]
    private partial void LogFailed(Exception exception, string tool);
}
