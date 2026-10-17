using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// Decorator that remembers probe results until the executable changes on disk (path, size or
/// modification time), so showing the status page doesn't start yt-dlp every time.
/// </summary>
public sealed class CachingToolVersionProbe : IToolVersionProbe
{
    private readonly IToolVersionProbe _inner;
    private readonly ConcurrentDictionary<string, (FileStamp Stamp, ToolProbeResult Result)> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingToolVersionProbe"/> class.
    /// </summary>
    /// <param name="inner">The probe that does the work.</param>
    public CachingToolVersionProbe(IToolVersionProbe inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public async Task<ToolProbeResult> ProbeAsync(IToolDefinition definition, string executablePath, CancellationToken cancellationToken)
    {
        var stamp = FileStamp.Of(executablePath);
        if (stamp is null)
        {
            // Missing files aren't cached: they may appear any moment.
            _cache.TryRemove(executablePath, out _);
            return await _inner.ProbeAsync(definition, executablePath, cancellationToken).ConfigureAwait(false);
        }

        if (_cache.TryGetValue(executablePath, out var cached) && cached.Stamp == stamp)
        {
            return cached.Result;
        }

        var result = await _inner.ProbeAsync(definition, executablePath, cancellationToken).ConfigureAwait(false);
        _cache[executablePath] = (stamp, result);
        return result;
    }

    private sealed record FileStamp(long Length, DateTime LastWriteTimeUtc)
    {
        public static FileStamp? Of(string path)
        {
            var file = new FileInfo(path);
            return file.Exists ? new FileStamp(file.Length, file.LastWriteTimeUtc) : null;
        }
    }
}
