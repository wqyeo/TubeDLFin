using System;

namespace Jellyfin.Plugin.TubeDLFin.Tools.Installation;

/// <summary>
/// A tool could not be installed. The message is meant to be shown to the admin.
/// </summary>
public class ToolInstallException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolInstallException"/> class.
    /// </summary>
    public ToolInstallException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolInstallException"/> class.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    public ToolInstallException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolInstallException"/> class.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The cause.</param>
    public ToolInstallException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
