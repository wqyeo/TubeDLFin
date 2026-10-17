using System;

namespace Jellyfin.Plugin.TubeDLFin.Tools;

/// <summary>
/// A tool TubeDLFin needs is not installed or not working. The message is meant for the admin.
/// </summary>
public class ToolNotReadyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolNotReadyException"/> class.
    /// </summary>
    public ToolNotReadyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolNotReadyException"/> class.
    /// </summary>
    /// <param name="message">What is missing and how to fix it.</param>
    public ToolNotReadyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolNotReadyException"/> class.
    /// </summary>
    /// <param name="message">What is missing and how to fix it.</param>
    /// <param name="innerException">The cause.</param>
    public ToolNotReadyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
