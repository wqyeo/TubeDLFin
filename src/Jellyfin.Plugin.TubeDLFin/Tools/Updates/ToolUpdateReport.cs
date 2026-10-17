using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Updates;

/// <summary>
/// The result of one update run over all tools.
/// </summary>
/// <param name="FinishedAt">When the run ended.</param>
/// <param name="Results">One entry per tool.</param>
public sealed record ToolUpdateReport(DateTimeOffset FinishedAt, IReadOnlyList<ToolUpdateResult> Results);
