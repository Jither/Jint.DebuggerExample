using Jint.Runtime.Debugger;

namespace JintDebuggerExample;

/// <summary>
/// Simple example of extending breakpoints. This one uses gdb's idea of a temporary breakpoint for single use - i.e.
/// it's removed when hit.
/// </summary>
internal class ExtendedBreakPoint : BreakPoint
{
    public bool Temporary { get; }

    public ExtendedBreakPoint(string? source, int line, int column, string? condition = null, bool temporary = false)
        : base(source, line, column, condition)
    {
        Temporary = temporary;
    }
}
