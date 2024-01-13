using System.Linq;
using System.Collections.Generic;
using Esprima;
using Esprima.Ast;

namespace JintDebuggerExample;

/// <summary>
/// Holds "metadata" information about a loaded script: Its source code, and valid breakpoint positions.
/// </summary>
internal class SourceInfo
{
    private static readonly char[] lineBreaks = new[] { '\n', '\r' };
    private readonly List<int> linePositions = new();
    private readonly List<Position> breakPointPositions;

    public string Id { get; }
    public string Source { get; }

    public SourceInfo(string id, string source, Esprima.Ast.Program ast)
    {
        Id = id;
        Source = source;
        LocateLines();
        breakPointPositions = CollectBreakPointPositions(ast);
    }

    public string GetLine(Position position)
    {
        // Note that in Esprima, and hence Jint, the first line is line 1. The first column is column 0.
        int lineStart = linePositions[position.Line - 1];

        // Don't include newline
        int lineEnd = linePositions[position.Line] - 1;

        // ... or carriage return, if it's there
        if (lineBreaks.Contains(Source[lineEnd]))
        {
            lineEnd--;
        }

        return Source[lineStart..lineEnd];
    }

    public Position FindNearestBreakPointPosition(Position position)
    {
        var positions = breakPointPositions;
        int index = positions.BinarySearch(position, EsprimaPositionComparer.Default);
        if (index < 0)
        {
            // Get the first break after the candidate position
            index = ~index;
        }

        // If we're past the last position, just use the last position
        if (index >= positions.Count)
        {
            index = positions.Count - 1;
        }
        return positions[index];
    }

    // Quick implementation of more memory efficient lookup of script lines than keeping a lines array.
    private void LocateLines()
    {
        int linePosition = 0;
        while (true)
        {
            linePositions.Add(linePosition);

            linePosition = Source.IndexOfAny(lineBreaks, linePosition);
            if (linePosition < 0)
            {
                break;
            }
            linePosition++;
            if (Source[linePosition - 1] == '\r')
            {
                // Carriage return may be followed by newline, which also needs skipping
                if (linePosition < Source.Length && Source[linePosition] == '\n')
                {
                    linePosition++;
                }
            }
        }
        linePositions.Add(Source.Length);
    }

    private List<Position> CollectBreakPointPositions(Esprima.Ast.Program ast)
    {
        var collector = new BreakPointCollector();
        collector.Visit(ast);

        // We need positions distinct and sorted (they'll be used in binary search)
        var positions = collector.Positions.Distinct().ToList();
        positions.Sort(EsprimaPositionComparer.Default);

        return positions;
    }
}
