using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Jellyfin.Plugin.TubeDLFin.YtDlp;

/// <summary>
/// Lenient reads of optional properties: yt-dlp leaves out or nulls whatever a site doesn't
/// provide, and a missing or wrongly typed value is treated as unknown rather than as an error.
/// </summary>
internal static class JsonElementReader
{
    /// <summary>
    /// Gets a property's value.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The value, or <c>null</c> if it is missing or JSON null.</returns>
    public static JsonElement? Property(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value
            : null;

    /// <summary>
    /// Gets a string property.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string, or <c>null</c> if it is missing, empty or not a string.</returns>
    public static string? String(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.String } value && value.GetString() is { Length: > 0 } text ? text : null;

    /// <summary>
    /// Gets a number property.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The number, or <c>null</c> if it is missing, not a number or not finite.</returns>
    public static double? Number(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number) && double.IsFinite(number)
            ? number
            : null;

    /// <summary>
    /// Gets a number property rounded to an <see cref="int"/>.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The number, or <c>null</c> if it is missing, not a number or out of range.</returns>
    public static int? Integer(this JsonElement element, string name) =>
        element.Number(name) is { } number && number is >= int.MinValue and <= int.MaxValue ? (int)Math.Round(number) : null;

    /// <summary>
    /// Gets a number property rounded to a <see cref="long"/>.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The number, or <c>null</c> if it is missing, not a number or out of range.</returns>
    public static long? LongInteger(this JsonElement element, string name) =>
        element.Number(name) is { } number && number is >= long.MinValue and <= long.MaxValue ? (long)Math.Round(number) : null;

    /// <summary>
    /// Gets an array property's items.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The items, or none if it is missing or not an array.</returns>
    public static IEnumerable<JsonElement> Array(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.Array } value ? value.EnumerateArray() : [];

    /// <summary>
    /// Gets an array property's non-empty strings.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The strings, or none if it is missing or not an array.</returns>
    public static IEnumerable<string> Strings(this JsonElement element, string name) =>
        element.Array(name).Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).Where(s => s.Length > 0);

    /// <summary>
    /// Gets an object property's members.
    /// </summary>
    /// <param name="element">The object to read from.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The members, or none if it is missing or not an object.</returns>
    public static IEnumerable<JsonProperty> Properties(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.Object } value ? value.EnumerateObject() : [];
}
