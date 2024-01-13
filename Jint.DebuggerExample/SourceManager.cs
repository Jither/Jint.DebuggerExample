using System.IO;
using System.Collections.Generic;
using Esprima;

namespace JintDebuggerExample;

/// <summary>
/// Manages the loaded scripts - although in this example debugger, there's only one.
/// It does, however, give access to <see cref="SourceInfo"/> which helps with source
/// line output and finding breakpoint locations.
/// </summary>
internal class SourceManager
{
    private readonly Dictionary<string, SourceInfo> sourceInfoById = new();

    public string Load(Esprima.Ast.Program ast, string sourceId, string path)
    {
        string script;
        try
        {
            script = File.ReadAllText(path);
            sourceInfoById.Add(sourceId, new SourceInfo(sourceId, script, ast));
            return script;
        }
        catch (IOException ex)
        {
            throw new ProgramException($"Script could not be read: {ex.Message}");
        }
    }

    public Position FindNearestBreakPointPosition(string sourceId, Position position)
    {
        var source = GetSourceInfo(sourceId);
        return source.FindNearestBreakPointPosition(position);
    }

    public string GetLine(Location location)
    {
        if (location.Source == null)
        {
            throw new ProgramException($"Location included no source ID");
        }
        // We gave Esprima our source ID when we executed the script - so we can get it back from the location
        var source = GetSourceInfo(location.Source);

        return source.GetLine(location.Start);
    }

    private SourceInfo GetSourceInfo(string sourceId)
    {
        if (!sourceInfoById.TryGetValue(sourceId, out var info))
        {
            throw new ProgramException($"Script with source ID '{sourceId}' was not found.");
        }
        return info;
    }
}
