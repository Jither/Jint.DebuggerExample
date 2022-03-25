using System;
using System.Diagnostics;
using System.Linq;
using Esprima;
using Jint;
using Jint.Native;
using Jint.Runtime.Debugger;
using JintDebuggerExample.Helpers;

namespace JintDebuggerExample;

/// <summary>
/// The main debugger logic, handling debugger commands and interacting with Engine.DebugHandler.
/// </summary>
internal class Debugger
{
    private readonly Engine engine;
    private readonly CommandLine commandLine;
    private readonly SourceManager sources;

    private StepMode stepMode = StepMode.Into;
    private DebugInformation? currentInfo;

    public Debugger()
    {
        commandLine = new CommandLine();
        sources = new SourceManager();

        commandLine.Register("Continue running", Continue, "continue", "c");
        commandLine.Register("Step into", StepInto, "into", "i");
        commandLine.Register("Step over", StepOver, "over", "o");
        commandLine.Register("Step out", StepOut, "out", "u");
        commandLine.Register("List breakpoints", InfoBreakPoints, "breaks");
        commandLine.Register("Set breakpoint", SetBreakPoint, "break", "b", parameters: "<line> [column]");
        commandLine.Register("Set temporary breakpoint (removed after hit)", SetTemporaryBreakPoint, "tbreak", "tb", parameters: "<line> [column]");
        commandLine.Register("Clear breakpoints", ClearBreakPoints, "clear");
        commandLine.Register("Delete breakpoint", DeleteBreakPoint, "delete", parameters: "<index>");
        commandLine.Register("List current call stack", InfoStack, "stack");
        commandLine.Register("List current scope chain", InfoScopes, "scopes");
        commandLine.Register("List bindings in scope", InfoScope, "scope", parameters: "<index>");
        commandLine.Register("Evaluate expression", Evaluate, "eval", "!", parameters: "<expression>");
        commandLine.Register("Help", Help, "help", "h");
        commandLine.Register("Exit debugger", Exit, "exit", "x");

        engine = new Engine(options => options
            .DebugMode()
            .DebuggerStatementHandling(DebuggerStatementHandling.Script)
            .InitialStepMode(stepMode)
        );

        engine.DebugHandler.Break += DebugHandler_Break;
        engine.DebugHandler.Step += DebugHandler_Step;
    }

    public void Execute(string path)
    {
        string script = sources.Load(path, path);
        // We include the path in the parser options - easy way of identifying what script we're in
        // for multi file scripts (although we don't support multiple scripts in this example).
        engine.Execute(script, new ParserOptions(path));
        commandLine.Output("Execution reached end of script.");
    }

    private bool StepInto(string args)
    {
        stepMode = StepMode.Into;
        return true;
    }

    private bool StepOut(string args)
    {
        stepMode = StepMode.Out;
        return true;
    }

    private bool StepOver(string args)
    {
        stepMode = StepMode.Over;
        return true;
    }

    private bool Continue(string args)
    {
        // Note that in some cases (and maybe eventually in this case), we may want to set StepMode.Into here, and
        // keep track of "running" vs "stepping" ourselves. This in order to be called regularly, and give the user
        // a chance to interactively pause the script when it's running.
        // For now, however, we stick with StepMode.None.
        stepMode = StepMode.None;
        return true;
    }

    private bool SetBreakPoint(string args)
    {
        var position = InternalSetBreakPoint(args, temporary: false);
        commandLine.Output($"Added breakpoint at {position}");

        return false;
    }

    private bool SetTemporaryBreakPoint(string args)
    {
        var position = InternalSetBreakPoint(args, temporary: true);
        commandLine.Output($"Added temporary breakpoint at {position}");

        return false;
    }

    private bool DeleteBreakPoint(string args)
    {
        var breakPoints = engine.DebugHandler.BreakPoints;
        int index = commandLine.ParseIndex(args, engine.DebugHandler.BreakPoints.Count);

        // Yeah, this is where I realize that BreakPointCollection should probably be ICollection
        var breakPoint = breakPoints.Skip(index).First();
        breakPoints.RemoveAt(breakPoint.Location);
        commandLine.Output($"Removed breakpoint");

        return false;
    }

    private bool ClearBreakPoints(string args)
    {
        engine.DebugHandler.BreakPoints.Clear();
        commandLine.Output($"All breakpoints cleared.");

        return false;
    }

    private bool InfoBreakPoints(string args)
    {
        if (engine.DebugHandler.BreakPoints.Count == 0)
        {
            commandLine.Output("No breakpoints set.");
            return false;
        }

        int index = 0;
        foreach (var breakPoint in engine.DebugHandler.BreakPoints)
        {
            string flags = " ";
            if (breakPoint is ExtendedBreakPoint extended)
            {
                flags = extended.Temporary ? "T" : " ";
            }
            string location = RenderLocation(breakPoint.Location.Source, breakPoint.Location.Line, breakPoint.Location.Column);
            commandLine.Output($"{index, -4} {flags}  {location}  {breakPoint.Condition}");
            index++;
        }

        return false;
    }

    private bool InfoStack(string args)
    {
        Debug.Assert(currentInfo != null);

        int index = 0;
        foreach (var frame in currentInfo.CallStack)
        {
            string location = RenderLocation(frame.Location);
            commandLine.Output($"{index, -4} {frame.FunctionName,-40}  {location}");
            index++;
        }

        return false;
    }

    private bool InfoScopes(string args)
    {
        Debug.Assert(currentInfo != null);

        int index = 0;
        foreach (var scope in currentInfo.CurrentScopeChain)
        {
            commandLine.Output($"{index, -4} {scope.ScopeType}");
            index++;
        }

        return false;
    }

    private bool InfoScope(string args)
    {
        Debug.Assert(currentInfo != null);

        int index = commandLine.ParseIndex(args, currentInfo.CurrentScopeChain.Count);

        var scope = currentInfo.CurrentScopeChain[index];

        commandLine.Output($"{scope.ScopeType} scope:");

        // For local scope, we output return value (if at a return point - i.e. if ReturnValue isn't null)
        // and "this" (if defined)
        if (scope.ScopeType == DebugScopeType.Local)
        {
            if (currentInfo.ReturnValue != null)
            {
                commandLine.OutputBinding("return value", currentInfo.ReturnValue);
            }
            if (!currentInfo.CurrentCallFrame.This.IsUndefined())
            {
                commandLine.OutputBinding("this", currentInfo.CurrentCallFrame.This);
            }
        }

        // And now all the scope's bindings ("variables")
        foreach (var name in scope.BindingNames)
        {
            JsValue value = scope.GetBindingValue(name);
            commandLine.OutputBinding(name, value);
        }

        return false;
    }

    private bool Evaluate(string args)
    {
        if (args == String.Empty)
        {
            throw new CommandException("No expression to evaluate.");
        }
        try
        {
            // DebugHandler.Evaluate allows us to evaluate the expression in the Engine's current execution context.
            var result = engine.DebugHandler.Evaluate(args);
            commandLine.OutputValue(result);
        }
        catch (DebugEvaluationException ex)
        {
            // InnerException is the original JavaScriptException or ParserException.
            // We want the message from those, if it's available.
            // In rare cases, other exceptions may be thrown by Jint - in those cases,
            // we display the DebugEvaluationException's own message.
            throw new CommandException(ex.InnerException?.Message ?? ex.Message);
        }

        return false;
    }

    private bool Help(string args)
    {
        commandLine.OutputHelp();

        return false;
    }

    private bool Exit(string args)
    {
        Environment.Exit(0);
        return false;
    }

    private Position InternalSetBreakPoint(string args, bool temporary)
    {
        Debug.Assert(currentInfo != null);

        var position = commandLine.ParseBreakPoint(args);

        // This is a bit of a cheat, since we're only dealing with one script, but some classes here are prepared
        // for many - we just use the source ID of the current location. We "know" Source is not null, but the compiler
        // doesn't:
        string sourceId = currentInfo.Location.Source ?? "";

        // Jint requires *exact* breakpoint positions (line/column of the Esprima node targeted)
        // SourceManager/SourceInfo includes code for achieving that (may eventually be part of Jint API):
        position = sources.FindNearestBreakPointPosition(sourceId, position);

        engine.DebugHandler.BreakPoints.Set(new ExtendedBreakPoint(sourceId, position.Line, position.Column, temporary: temporary));

        return position;
    }

    private StepMode DebugHandler_Step(object sender, DebugInformation e)
    {
        Pause(e);
        return stepMode;
    }

    private StepMode DebugHandler_Break(object sender, DebugInformation e)
    {
        if (e.BreakPoint is ExtendedBreakPoint breakPoint)
        {
            // Temporary breakpoints are removed when hit
            if (breakPoint.Temporary)
            {
                engine.DebugHandler.BreakPoints.RemoveAt(e.BreakPoint.Location);
            }
        }
        Pause(e);
        return stepMode;
    }

    private void Pause(DebugInformation e)
    {
        currentInfo = e;
        string line = sources.GetLine(e.Location);

        // Output the location we're at:
        commandLine.OutputPosition(e.Location, line);

        // In this - single threaded - example debugger, we let Console.ReadLine take care of blocking the execution.
        // In debuggers involving a UI or in a debug server, we'd need script execution to be on a separate thread
        // from the UI/server, and use e.g. a ManualResetEvent, or a message queue loop here to block until the user
        // signals that the execution should continue.
        commandLine.Input();
    }

    private string RenderLocation(Location location)
    {
        string? source = location.Source?.CropStart(20);
        int line = location.Start.Line;
        int column = location.Start.Column;
        return RenderLocation(source, line, column);
    }

    private string RenderLocation(string? source, int line, int column)
    {
        return $"{source,-20} {line,4}:{column,4}";
    }
}
