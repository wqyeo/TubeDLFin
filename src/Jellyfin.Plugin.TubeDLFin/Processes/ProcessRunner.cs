using System;
using System.Diagnostics;
using System.IO;
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

        using var process = Start(request);

        // Read both streams at the same time, so a full stderr buffer can't block stdout.
        var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);

        var timedOut = await WaitForExitAsync(process, request.Timeout, cancellationToken).ConfigureAwait(false);
        return new ProcessResult(
            timedOut ? -1 : process.ExitCode,
            await stdout.ConfigureAwait(false),
            await stderr.ConfigureAwait(false),
            timedOut);
    }

    /// <inheritdoc />
    public async Task<ProcessExit> StreamAsync(ProcessRequest request, Action<ProcessOutputLine> onLine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onLine);

        using var process = Start(request);
        var stdout = PumpAsync(process.StandardOutput, isError: false, onLine);
        var stderr = PumpAsync(process.StandardError, isError: true, onLine);

        bool timedOut;
        try
        {
            timedOut = await WaitForExitAsync(process, request.Timeout, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // The streams close when the process ends (or is killed): let every line through
            // before returning, so no callback runs after the caller moved on.
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        }

        return new ProcessExit(timedOut ? -1 : process.ExitCode, timedOut);
    }

    private static Process Start(ProcessRequest request)
    {
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

        var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch
        {
            process.Dispose();
            throw;
        }

        // Nothing is ever sent to the program; closing stdin stops it waiting for input.
        process.StandardInput.Close();
        return process;
    }

    // Returns true if the timeout ran out. Kills the process tree on timeout or cancellation,
    // and rethrows if the caller cancelled.
    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        using var timer = timeout is { } limit ? new CancellationTokenSource(limit) : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timer.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            KillTree(process);

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return true;
        }
    }

    private static async Task PumpAsync(StreamReader reader, bool isError, Action<ProcessOutputLine> onLine)
    {
        while (await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) is { } line)
        {
            onLine(new ProcessOutputLine(line, isError));
        }
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
