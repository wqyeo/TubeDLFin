using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Processes;

/// <summary>
/// A program to start. Arguments are passed one by one, never through a shell, so nothing in
/// them can be read as shell syntax.
/// </summary>
/// <param name="FileName">The executable to start.</param>
/// <param name="Arguments">The arguments, one entry per argument.</param>
public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments)
{
    /// <summary>
    /// Gets how long the program may run before it is killed. <c>null</c> means no limit.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the working directory, or <c>null</c> for the server's current directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }
}
