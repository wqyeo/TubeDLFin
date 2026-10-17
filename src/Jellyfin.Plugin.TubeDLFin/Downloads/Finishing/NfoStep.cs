using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Finishing;

/// <summary>
/// Writes the <c>.nfo</c> files for the library's type, if the profile asks for them.
/// </summary>
public sealed class NfoStep : IFinishedItemStep
{
    private static readonly XmlWriterSettings _settings = new()
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = true,
    };

    private readonly IEnumerable<INfoBuilder> _builders;

    /// <summary>
    /// Initializes a new instance of the <see cref="NfoStep"/> class.
    /// </summary>
    /// <param name="builders">One builder per kind of library.</param>
    public NfoStep(IEnumerable<INfoBuilder> builders)
    {
        _builders = builders ?? throw new ArgumentNullException(nameof(builders));
    }

    /// <inheritdoc />
    public void Run(DownloadPlan plan, DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(item);

        if (!plan.Profile.Extras.WriteNfo)
        {
            return;
        }

        foreach (var file in _builders.Where(b => b.AppliesTo(plan.Library.Type)).SelectMany(b => b.Build(item)))
        {
            if (!file.Overwrite && File.Exists(file.Path))
            {
                continue;
            }

            // Write next to the target, then move: Jellyfin never reads a half-written file.
            var temp = file.Path + ".tmp";
            using (var writer = XmlWriter.Create(temp, _settings))
            {
                file.Document.Save(writer);
            }

            File.Move(temp, file.Path, overwrite: true);
        }
    }
}
