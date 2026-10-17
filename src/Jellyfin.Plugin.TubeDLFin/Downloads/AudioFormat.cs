namespace Jellyfin.Plugin.TubeDLFin.Downloads;

/// <summary>
/// The file type for audio downloads.
/// </summary>
public enum AudioFormat
{
    /// <summary>Keep the site's audio as it is (no re-encoding).</summary>
    Original,

    /// <summary>AAC in M4A.</summary>
    M4a,

    /// <summary>MP3.</summary>
    Mp3,

    /// <summary>Opus.</summary>
    Opus,

    /// <summary>FLAC (lossless container; doesn't add quality the source lacks).</summary>
    Flac,

    /// <summary>WAV (uncompressed).</summary>
    Wav,
}
