using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tools;
using Jellyfin.Plugin.TubeDLFin.YtDlp;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// <see cref="IMediaProber"/> running <c>yt-dlp --dump-single-json --flat-playlist</c>.
/// </summary>
public sealed partial class YtDlpMediaProber : IMediaProber
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    private readonly IYtDlpCommandFactory _commands;
    private readonly IProcessRunner _processRunner;
    private readonly IProbeResultParser _parser;
    private readonly IEnumerable<IPlaylistLinkInspector> _linkInspectors;
    private readonly IYtDlpErrorExplainer _explainer;
    private readonly ILogger<YtDlpMediaProber> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="YtDlpMediaProber"/> class.
    /// </summary>
    /// <param name="commands">Builds the yt-dlp command.</param>
    /// <param name="processRunner">Runs yt-dlp.</param>
    /// <param name="parser">Reads yt-dlp's JSON.</param>
    /// <param name="linkInspectors">Recognise links to a video inside a playlist.</param>
    /// <param name="explainer">Adds advice to yt-dlp's errors.</param>
    /// <param name="logger">The logger.</param>
    public YtDlpMediaProber(
        IYtDlpCommandFactory commands,
        IProcessRunner processRunner,
        IProbeResultParser parser,
        IEnumerable<IPlaylistLinkInspector> linkInspectors,
        IYtDlpErrorExplainer explainer,
        ILogger<YtDlpMediaProber> logger)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _linkInspectors = linkInspectors ?? throw new ArgumentNullException(nameof(linkInspectors));
        _explainer = explainer ?? throw new ArgumentNullException(nameof(explainer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ProbeResult> ProbeAsync(MediaUrl url, bool noPlaylist, int entryLimit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentOutOfRangeException.ThrowIfLessThan(entryLimit, 1);

        var options = new List<string>
        {
            "--dump-single-json",
            "--flat-playlist",
            "--playlist-items",
            string.Create(CultureInfo.InvariantCulture, $"1:{entryLimit}"),
        };
        if (noPlaylist)
        {
            options.Add("--no-playlist");
        }

        var request = await _commands.CreateAsync(options, url, cancellationToken).ConfigureAwait(false) with { Timeout = Timeout };

        ProcessResult result;
        try
        {
            result = await _processRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception ex)
        {
            throw new ToolNotReadyException($"Could not start yt-dlp: {ex.Message}", ex);
        }

        if (result.TimedOut)
        {
            throw new ProbeFailedException(ProbeFailure.TimedOut, $"yt-dlp did not finish within {Timeout.TotalMinutes:0} minutes. The site may be slow or blocking this server.");
        }

        var error = YtDlpMessages.LastError(result.StandardError);
        var json = result.StandardOutput.Trim();

        // On failure yt-dlp prints "null". Partial failures (some playlist items unavailable)
        // still print the JSON, so a non-zero exit code alone isn't fatal.
        if (json.Length == 0 || json == "null")
        {
            LogRejected(url.Value, result.ExitCode, error ?? "no error message");
            throw new ProbeFailedException(ProbeFailure.Rejected, error is null ? $"yt-dlp exited with code {result.ExitCode} without saying why." : _explainer.Explain(error));
        }

        ProbeResult parsed;
        try
        {
            parsed = _parser.Parse(json, entryLimit);
        }
        catch (JsonException ex)
        {
            LogUnreadable(ex, url.Value);
            throw new ProbeFailedException(ProbeFailure.UnreadableOutput, "TubeDLFin could not read yt-dlp's output. Try updating yt-dlp under Tools & settings.", ex);
        }

        var warnings = YtDlpMessages.Warnings(result.StandardError).ToList();
        if (error is not null)
        {
            warnings.Add(error);
        }

        return parsed with
        {
            HasPlaylistChoice = _linkInspectors.Any(inspector => inspector.HasPlaylistChoice(url)),
            Warnings = warnings,
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "yt-dlp could not detect {Url} (exit code {ExitCode}): {Error}")]
    private partial void LogRejected(Uri url, int exitCode, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read yt-dlp's JSON for {Url}")]
    private partial void LogUnreadable(Exception exception, Uri url);
}
