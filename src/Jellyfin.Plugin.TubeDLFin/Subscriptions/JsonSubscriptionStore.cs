using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Platform;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeDLFin.Subscriptions;

/// <summary>
/// <see cref="ISubscriptionStore"/> writing <c>subscriptions.json</c> in the plugin's data folder,
/// with a format version so it can change later. Writes go to a temporary file that then replaces
/// the old one, so a crash can't leave half a file. A file that can't be read is renamed and kept,
/// never overwritten.
/// </summary>
public sealed partial class JsonSubscriptionStore : ISubscriptionStore
{
    /// <summary>
    /// The file format this version writes and reads.
    /// </summary>
    public const int FormatVersion = 1;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IPluginPaths _paths;
    private readonly TimeProvider _time;
    private readonly ILogger<JsonSubscriptionStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSubscriptionStore"/> class.
    /// </summary>
    /// <param name="paths">The plugin's folders.</param>
    /// <param name="time">The clock, to name a file that is set aside.</param>
    /// <param name="logger">The logger.</param>
    public JsonSubscriptionStore(IPluginPaths paths, TimeProvider time, ILogger<JsonSubscriptionStore> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> LoadAsync(CancellationToken cancellationToken)
    {
        var path = _paths.SubscriptionsFile;
        if (!File.Exists(path))
        {
            return [];
        }

        SubscriptionFile? file;
        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                file = await JsonSerializer.DeserializeAsync<SubscriptionFile>(stream, _options, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            return SetAside(path, ex.Message);
        }

        if (file?.Version != FormatVersion || file.Subscriptions is null)
        {
            return SetAside(path, $"format version {file?.Version.ToString(CultureInfo.InvariantCulture) ?? "missing"}, this TubeDLFin reads version {FormatVersion}");
        }

        return file.Subscriptions;
    }

    /// <inheritdoc />
    public async Task SaveAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        var path = _paths.SubscriptionsFile;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        var stream = File.Create(temporary);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, new SubscriptionFile(FormatVersion, subscriptions), _options, cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporary, path, overwrite: true);
    }

    private List<Subscription> SetAside(string path, string reason)
    {
        var aside = $"{path}.unreadable-{_time.GetUtcNow():yyyyMMdd-HHmmss}";
        File.Move(path, aside);
        LogSetAside(path, reason, aside);
        return [];
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not read {Path} ({Reason}). It was renamed to {Aside}; TubeDLFin starts without subscriptions.")]
    private partial void LogSetAside(string path, string reason, string aside);

    private sealed record SubscriptionFile(int Version, IReadOnlyList<Subscription>? Subscriptions);
}
