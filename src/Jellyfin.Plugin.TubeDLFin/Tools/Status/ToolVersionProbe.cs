using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Processes;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Status;

/// <summary>
/// <see cref="IToolVersionProbe"/> that starts the executable through <see cref="IProcessRunner"/>.
/// </summary>
public sealed class ToolVersionProbe : IToolVersionProbe
{
    // The single-file yt-dlp build unpacks itself on every start: slow on a Raspberry Pi.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolVersionProbe"/> class.
    /// </summary>
    /// <param name="processRunner">Starts the executable.</param>
    public ToolVersionProbe(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public async Task<ToolProbeResult> ProbeAsync(IToolDefinition definition, string executablePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!File.Exists(executablePath))
        {
            return ToolProbeResult.Failed($"File not found: {executablePath}");
        }

        ProcessResult result;
        try
        {
            result = await _processRunner
                .RunAsync(new ProcessRequest(executablePath, definition.VersionArguments) { Timeout = Timeout }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Win32Exception ex)
        {
            return ToolProbeResult.Failed($"Could not start {executablePath}: {ex.Message}");
        }

        if (result.TimedOut)
        {
            return ToolProbeResult.Failed($"{definition.DisplayName} did not answer within {Timeout.TotalSeconds:0} seconds.");
        }

        if (!result.Succeeded)
        {
            var detail = FirstLine(result.StandardError) ?? FirstLine(result.StandardOutput) ?? "no output";
            return ToolProbeResult.Failed($"{definition.DisplayName} exited with code {result.ExitCode}: {detail}");
        }

        var version = definition.ParseVersion(result.StandardOutput);
        return version is null
            ? ToolProbeResult.Failed($"Unrecognised {definition.DisplayName} version output: {FirstLine(result.StandardOutput)}")
            : ToolProbeResult.Ok(version);
    }

    private static string? FirstLine(string text)
    {
        var line = text.Trim().Split('\n', 2)[0].Trim();
        return line.Length == 0 ? null : line;
    }
}
