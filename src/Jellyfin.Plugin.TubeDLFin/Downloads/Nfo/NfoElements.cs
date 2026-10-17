using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Jellyfin.Plugin.TubeDLFin.Downloads.Output;

namespace Jellyfin.Plugin.TubeDLFin.Downloads.Nfo;

/// <summary>
/// Pieces shared by the <c>.nfo</c> builders, in the Kodi format Jellyfin reads.
/// </summary>
public static class NfoElements
{
    /// <summary>
    /// Gets the <c>.nfo</c> path for a media file: same name, <c>.nfo</c> extension.
    /// </summary>
    /// <param name="mediaPath">The media file.</param>
    /// <returns>The <c>.nfo</c> path.</returns>
    public static string PathFor(string mediaPath) => Path.ChangeExtension(mediaPath, ".nfo");

    /// <summary>
    /// Wraps a root element in a document with the usual declaration.
    /// </summary>
    /// <param name="root">The root element.</param>
    /// <returns>The document.</returns>
    public static XDocument Document(XElement root) => new(new XDeclaration("1.0", "utf-8", "yes"), root);

    /// <summary>
    /// Builds a text element, or nothing for a missing value. Characters XML can't hold
    /// (control characters in descriptions) are removed.
    /// </summary>
    /// <param name="name">The element name.</param>
    /// <param name="value">The value.</param>
    /// <returns>The element, or <c>null</c>.</returns>
    public static XElement? Text(string name, string? value)
    {
        var clean = value is null ? null : new string(value.Where(XmlConvert.IsXmlChar).ToArray()).Trim();
        return string.IsNullOrEmpty(clean) ? null : new XElement(name, clean);
    }

    /// <summary>
    /// Builds a date element (<c>yyyy-MM-dd</c>).
    /// </summary>
    /// <param name="name">The element name.</param>
    /// <param name="date">The date.</param>
    /// <returns>The element, or <c>null</c>.</returns>
    public static XElement? Date(string name, DateOnly? date) =>
        date is { } value ? new XElement(name, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : null;

    /// <summary>
    /// Builds a number element.
    /// </summary>
    /// <param name="name">The element name.</param>
    /// <param name="value">The value.</param>
    /// <returns>The element, or <c>null</c>.</returns>
    public static XElement? Number(string name, long? value) =>
        value is { } number ? new XElement(name, number.ToString(CultureInfo.InvariantCulture)) : null;

    /// <summary>
    /// Builds the runtime in whole minutes (at least 1).
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The element, or <c>null</c>.</returns>
    public static XElement? Runtime(DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Duration is > 0 and var seconds ? Number("runtime", Math.Max(1, (long)Math.Round(seconds / 60))) : null;
    }

    /// <summary>
    /// Builds the site id element, e.g. <c>&lt;uniqueid type="youtube"&gt;</c>, which Jellyfin
    /// shows as a provider id.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The element, or <c>null</c>.</returns>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Jellyfin and Kodi use lower-case provider names.")]
    public static XElement? UniqueId(DownloadedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Id is null)
        {
            return null;
        }

        var site = (item.Extractor ?? "web").ToLowerInvariant();
        return new XElement("uniqueid", new XAttribute("type", site), new XAttribute("default", "true"), item.Id);
    }

    /// <summary>
    /// Builds <c>&lt;lockdata&gt;true&lt;/lockdata&gt;</c>: Jellyfin keeps these details and
    /// doesn't replace them with an online match (the admin can still edit them).
    /// </summary>
    /// <returns>The element.</returns>
    public static XElement LockData() => new("lockdata", "true");
}
