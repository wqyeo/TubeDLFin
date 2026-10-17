using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Jellyfin.Plugin.TubeDLFin.Tools.Definitions;
using Jellyfin.Plugin.TubeDLFin.Tools.Releases;
using Jellyfin.Plugin.TubeDLFin.Tools.Status;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Installation;

/// <summary>
/// <see cref="IToolInstaller"/> that stages everything in a private temporary folder next to
/// <c>bin</c>, then swaps the executable in with a single rename.
/// </summary>
public sealed class ToolInstaller : IToolInstaller
{
    private const UnixFileMode Executable =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

    private readonly IReleaseClient _releases;
    private readonly IToolVersionProbe _probe;
    private readonly IPluginPaths _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolInstaller"/> class.
    /// </summary>
    /// <param name="releases">Downloads release files.</param>
    /// <param name="probe">Checks that the downloaded tool runs.</param>
    /// <param name="paths">The plugin's folders.</param>
    public ToolInstaller(IReleaseClient releases, IToolVersionProbe probe, IPluginPaths paths)
    {
        _releases = releases ?? throw new ArgumentNullException(nameof(releases));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <inheritdoc />
    public async Task<string> InstallAsync(IToolDefinition definition, ToolAsset asset, string tag, string destinationPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(asset);

        var staging = Directory.CreateDirectory(Path.Combine(_paths.TempDirectory, "tools", Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var downloaded = Path.Combine(staging, asset.FileName);
            await _releases.DownloadAssetAsync(definition.Repository, tag, asset.FileName, downloaded, cancellationToken).ConfigureAwait(false);
            await VerifyChecksumAsync(definition, asset, tag, downloaded, cancellationToken).ConfigureAwait(false);

            var executable = asset.ArchiveEntry is null
                ? downloaded
                : await ExtractAsync(asset, downloaded, Path.Combine(staging, "extracted"), cancellationToken).ConfigureAwait(false);

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(executable, Executable);
            }

            var probe = await _probe.ProbeAsync(definition, executable, cancellationToken).ConfigureAwait(false);
            if (!probe.Works)
            {
                throw new ToolInstallException($"{definition.DisplayName} {tag} was downloaded but doesn't run on this system: {probe.Error}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Move(executable, destinationPath, overwrite: true);
            return probe.Version!;
        }
        finally
        {
            TryDelete(staging);
        }
    }

    private static async Task<string> ExtractAsync(ToolAsset asset, string archivePath, string targetDirectory, CancellationToken cancellationToken)
    {
        var archive = await ZipFile.OpenReadAsync(archivePath, cancellationToken).ConfigureAwait(false);
        await using (archive.ConfigureAwait(false))
        {
            var entry = archive.GetEntry(asset.ArchiveEntry!)
                ?? throw new ToolInstallException($"{asset.FileName} doesn't contain {asset.ArchiveEntry}.");

            Directory.CreateDirectory(targetDirectory);
            var target = Path.Combine(targetDirectory, Path.GetFileName(asset.ArchiveEntry!));
            await entry.ExtractToFileAsync(target, overwrite: true, cancellationToken).ConfigureAwait(false);
            return target;
        }
    }

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // Leftovers in tmp/ are harmless and cleared on a later run.
        }
        catch (UnauthorizedAccessException)
        {
            // Same as above.
        }
    }

    private async Task VerifyChecksumAsync(IToolDefinition definition, ToolAsset asset, string tag, string file, CancellationToken cancellationToken)
    {
        var list = await _releases.GetAssetTextAsync(definition.Repository, tag, asset.ChecksumFileName, cancellationToken).ConfigureAwait(false);
        var expected = ChecksumList.Parse(list).Find(asset.FileName)
            ?? throw new ToolInstallException($"{asset.ChecksumFileName} of {definition.DisplayName} {tag} has no entry for {asset.FileName}.");

        var stream = File.OpenRead(file);
        string actual;
        await using (stream.ConfigureAwait(false))
        {
            actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
        }

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new ToolInstallException(
                $"Checksum mismatch for {asset.FileName} ({definition.DisplayName} {tag}): expected {expected}, got {actual}. The download was not installed.");
        }
    }
}
