using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Configuration;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// <see cref="IYtDlpCommandFactory"/> that checks the tools through <see cref="IToolStatusService"/>.
/// </summary>
public sealed class YtDlpCommandFactory : IYtDlpCommandFactory
{
    private readonly IToolStatusService _tools;
    private readonly IFfmpegLocator _ffmpeg;
    private readonly IPluginPaths _paths;
    private readonly IPluginConfigurationProvider _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="YtDlpCommandFactory"/> class.
    /// </summary>
    /// <param name="tools">Reports whether yt-dlp and Deno work.</param>
    /// <param name="ffmpeg">Finds Jellyfin's FFmpeg.</param>
    /// <param name="paths">The plugin's folders.</param>
    /// <param name="configuration">The settings (cookies file, extra arguments).</param>
    public YtDlpCommandFactory(IToolStatusService tools, IFfmpegLocator ffmpeg, IPluginPaths paths, IPluginConfigurationProvider configuration)
    {
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        _ffmpeg = ffmpeg ?? throw new ArgumentNullException(nameof(ffmpeg));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<ProcessRequest> CreateAsync(IEnumerable<string> options, MediaUrl url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(url);

        var ytDlp = await _tools.GetStatusAsync(ToolKind.YtDlp, cancellationToken).ConfigureAwait(false);
        if (ytDlp.State != ToolState.Ready)
        {
            throw new ToolNotReadyException(NotReadyMessage(ytDlp));
        }

        var arguments = new List<string>
        {
            // Ignore any yt-dlp config file on the server: TubeDLFin's options must be the only ones.
            "--ignore-config",
            "--no-color",
            "--cache-dir",
            _paths.CacheDirectory,
        };

        // Without Deno, YouTube may offer fewer formats, but other sites work, so it's not required.
        var deno = await _tools.GetStatusAsync(ToolKind.Deno, cancellationToken).ConfigureAwait(false);
        if (deno.State == ToolState.Ready)
        {
            arguments.Add("--js-runtimes");
            arguments.Add("deno:" + deno.Path);
        }

        var ffmpeg = _ffmpeg.GetStatus();
        if (ffmpeg.Found)
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(ffmpeg.EncoderPath!);
        }

        var settings = _configuration.Current;
        if (!string.IsNullOrWhiteSpace(settings.CookiesFile))
        {
            var cookies = settings.CookiesFile.Trim();
            if (!File.Exists(cookies))
            {
                throw new ToolNotReadyException($"The cookies file \"{cookies}\" set under Tools & settings does not exist.");
            }

            arguments.Add("--cookies");
            arguments.Add(cookies);
        }

        arguments.AddRange(options);

        // The admin's own arguments come last, so they win over TubeDLFin's.
        try
        {
            arguments.AddRange(ArgumentSplitter.Split(settings.ExtraArguments));
        }
        catch (FormatException ex)
        {
            throw new ToolNotReadyException(ex.Message + " Fix them under Tools & settings.", ex);
        }

        arguments.Add("--");
        arguments.Add(url.Value.AbsoluteUri);

        return new ProcessRequest(ytDlp.Path, arguments);
    }

    private static string NotReadyMessage(ToolStatus ytDlp) => ytDlp.State switch
    {
        ToolState.NotInstalled => "yt-dlp is not installed yet. TubeDLFin installs it at server start; "
            + "check Tools & settings, or press \"Update now\" there.",
        ToolState.Unsupported => ytDlp.Message ?? "yt-dlp is not available for this system.",
        _ => $"yt-dlp is not working: {ytDlp.Message ?? "unknown error"}. Check Tools & settings.",
    };
}
