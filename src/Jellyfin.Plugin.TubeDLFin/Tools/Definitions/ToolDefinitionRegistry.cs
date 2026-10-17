using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

/// <summary>
/// <see cref="IToolDefinitionRegistry"/> over the definitions registered in DI.
/// </summary>
public sealed class ToolDefinitionRegistry : IToolDefinitionRegistry
{
    private readonly Dictionary<ToolKind, IToolDefinition> _byKind;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolDefinitionRegistry"/> class.
    /// </summary>
    /// <param name="definitions">The definitions; each kind may appear once.</param>
    public ToolDefinitionRegistry(IEnumerable<IToolDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        All = definitions.OrderBy(d => d.Kind).ToArray();
        _byKind = All.ToDictionary(d => d.Kind);
    }

    /// <inheritdoc />
    public IReadOnlyList<IToolDefinition> All { get; }

    /// <inheritdoc />
    public IToolDefinition GetDefinition(ToolKind kind) =>
        _byKind.TryGetValue(kind, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "No definition registered for this tool.");
}
