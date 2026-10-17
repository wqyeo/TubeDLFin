using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Updates;

/// <summary>
/// Scheduled task (Dashboard → Scheduled Tasks) that installs missing tools at server start and
/// keeps them up to date daily; YouTube changes often break older yt-dlp releases. Admins can
/// change or remove the triggers there.
/// </summary>
public sealed class UpdateToolsTask : IScheduledTask
{
    private readonly IToolUpdateService _updates;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateToolsTask"/> class.
    /// </summary>
    /// <param name="updates">The update service.</param>
    public UpdateToolsTask(IToolUpdateService updates)
    {
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
    }

    /// <inheritdoc />
    public string Name => "Update yt-dlp and Deno";

    /// <inheritdoc />
    public string Key => "TubeDLFinUpdateTools";

    /// <inheritdoc />
    public string Description => "Installs or updates the yt-dlp and Deno copies that TubeDLFin manages. Tools with a custom path are left alone.";

    /// <inheritdoc />
    public string Category => PluginIdentity.Name;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) =>
        _updates.UpdateAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() =>
    [
        new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger },
        new TaskTriggerInfo { Type = TaskTriggerInfoType.DailyTrigger, TimeOfDayTicks = TimeSpan.FromHours(4).Ticks },
    ];
}
