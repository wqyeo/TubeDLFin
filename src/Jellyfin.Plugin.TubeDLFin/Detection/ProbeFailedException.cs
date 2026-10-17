using System;

namespace Jellyfin.Plugin.TubeDLFin.Detection;

/// <summary>
/// A link could not be detected. The message is meant for the admin.
/// </summary>
public class ProbeFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeFailedException"/> class.
    /// </summary>
    public ProbeFailedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeFailedException"/> class.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    public ProbeFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeFailedException"/> class.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The cause.</param>
    public ProbeFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbeFailedException"/> class.
    /// </summary>
    /// <param name="reason">Why it failed.</param>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The cause, if any.</param>
    public ProbeFailedException(ProbeFailure reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets why it failed.
    /// </summary>
    public ProbeFailure Reason { get; }
}
