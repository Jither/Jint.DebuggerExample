using System.Collections.Generic;
using Esprima;

namespace JintDebuggerExample;

/// <summary>
/// Does comparison of Esprima positions (line/column) for binary search - until Esprima.Position might get an
/// IComparable interface.
/// </summary>
internal class EsprimaPositionComparer : IComparer<Position>
{
    public static readonly EsprimaPositionComparer Default = new();

    public int Compare(Position a, Position b)
    {
        if (a.Line != b.Line)
        {
            return a.Line - b.Line;
        }
        return a.Column - b.Column;
    }
}