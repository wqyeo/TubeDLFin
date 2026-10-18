using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.YtDlp;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// Decorator that lets only a few detections run at once, so repeated clicks can't start a
/// pile of yt-dlp processes on a small server. Later callers wait their turn.
/// </summary>
public sealed class ThrottledMediaProber : IMediaProber, IDisposable
{
    private readonly IMediaProber _inner;
    private readonly SemaphoreSlim _slots;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottledMediaProber"/> class.
    /// </summary>
    /// <param name="inner">The prober doing the work.</param>
    /// <param name="maxConcurrent">How many detections may run at once.</param>
    public ThrottledMediaProber(IMediaProber inner, int maxConcurrent)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrent, 1);

        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _slots = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    /// <inheritdoc />
    public async Task<ProbeResult> ProbeAsync(MediaUrl url, bool noPlaylist, int entryLimit, CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _inner.ProbeAsync(url, noPlaylist, entryLimit, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _slots.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _slots.Dispose();
}
