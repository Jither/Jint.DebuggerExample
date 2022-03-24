using System;

namespace JintDebuggerExample;

/// <summary>
/// Thrown on debugger command syntax errors.
/// </summary>
internal class CommandException : Exception
{
    public CommandException(string? message) : base(message)
    {
    }
}
