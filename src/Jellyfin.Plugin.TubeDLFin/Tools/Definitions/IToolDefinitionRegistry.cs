using System.Collections.Generic;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Definitions;

/// <summary>
/// All registered <see cref="IToolDefinition"/>s, looked up by <see cref="ToolKind"/>.
/// </summary>
public interface IToolDefinitionRegistry
{
    /// <summary>
    /// Gets every definition, in a stable order.
    /// </summary>
    IReadOnlyList<IToolDefinition> All { get; }

    /// <summary>
    /// Gets the definition of a tool.
    /// </summary>
    /// <param name="kind">The tool.</param>
    /// <returns>The definition.</returns>
    IToolDefinition GetDefinition(ToolKind kind);
}
