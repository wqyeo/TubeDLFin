using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Processes;

/// <summary>
/// <see cref="IProcessRunner"/> backed by <see cref="Process"/>.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startInfo = new ProcessStartInfo(request.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (request.WorkingDirectory is not null)
        {
            startInfo.WorkingDirectory = request.WorkingDirectory;
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Nothing is ever sent to the program; closing stdin stops it waiting for input.
        process.StandardInput.Close();

        using var timeout = request.Timeout is { } limit ? new CancellationTokenSource(limit) : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        // Read both streams at the same time, so a full stderr buffer can't block stdout.
        var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            KillTree(process);

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return new ProcessResult(-1, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false), TimedOut: true);
        }

        return new ProcessResult(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false), TimedOut: false);
    }

    private static void KillTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }
}
