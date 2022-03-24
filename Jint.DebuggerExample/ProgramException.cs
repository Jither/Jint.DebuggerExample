using System;

namespace JintDebuggerExample;

/// <summary>
/// Thrown for common fatal program errors that should get a nice message rather than a stack trace.
/// </summary>
internal class ProgramException : Exception
{
    public ProgramException(string? message) : base(message)
    {
    }
}
