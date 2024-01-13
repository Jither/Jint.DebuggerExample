using Jint.Native;
using Jint.Runtime;
using JintDebuggerExample;
using JintDebuggerExample.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jint.DebuggerExample;

internal class ConsoleObject
{
    internal enum LogType
    {
        Debug,
        Log,
        Info,
        Warn,
        Error,
    }

    private readonly int engineThreadId;
    private readonly CommandLine commandLine;
    private readonly Dictionary<string, long> timers = new();
    private readonly Dictionary<string, uint> counters = new();
    private readonly ValueRenderer renderer = new();

    public ConsoleObject(CommandLine commandLine)
    {
        this.commandLine = commandLine;
        this.engineThreadId = Environment.CurrentManagedThreadId;
    }

    public void Assert(JsValue assertion, params JsValue[] values)
    {
        if (!TypeConverter.ToBoolean(assertion))
        {
            Error(new JsValue[] { "Assertion failed:" }.Concat(values).ToArray());
        }
    }

    public void Clear()
    {
        commandLine.Clear();
    }

    public void Count(string label = null)
    {
        label ??= "default";

        if (!counters.TryGetValue(label, out var count))
        {
            count = 0;
        }
        count++;
        counters[label] = count;
        Log($"{label}: {count}");
    }

    public void CountReset(string label = null)
    {
        label ??= "default";

        if (!counters.ContainsKey(label))
        {
            Warn($"Count for '{label}' does not exist.");
            return;
        }

        counters[label] = 0;
        Log($"{label}: 0");
    }

    public void Debug(params JsValue[] values)
    {
        Log(LogType.Debug, values);
    }

    // TODO: Dir(), DirXml()

    public void Error(params JsValue[] values)
    {
        Log(LogType.Error, values);
    }

    // TODO: Groups
    /*
    public void Group(string label)
    {
        InternalSend(OutputCategory.Stdout, label, group: OutputGroup.Start);
    }

    public void GroupCollapsed(string label)
    {
        InternalSend(OutputCategory.Stdout, label, group: OutputGroup.StartCollapsed);
    }

    public void GroupEnd()
    {
        InternalSend(OutputCategory.Stdout, String.Empty, group: OutputGroup.End);
    }
    */

    public void Info(params JsValue[] values)
    {
        Log(LogType.Info, values);
    }

    public void Log(params JsValue[] values)
    {
        Log(LogType.Log, values);
    }

    private void Log(LogType type, params JsValue[] values)
    {
        var valuesString = String.Join(' ', values.Select(v => renderer.RenderValue(v, renderProperties: true)));
        uint color = type switch
        {
            LogType.Log => 0x40a4d8,
            LogType.Error => 0xdc3839,
            LogType.Warn => 0xfecc2f,
            LogType.Info => 0xb2c444,
            _ => 0xa0a0a0
        };
        string typeName = type.ToString().ToLowerInvariant().PadRight(5, ' ');
        commandLine.Output($"[{ConsoleHelpers.Color(typeName, color)}] {valuesString}");
    }

    // TODO: Table()

    public void Time(string label = null)
    {
        label ??= "default";

        timers[label] = Stopwatch.GetTimestamp();
    }

    public void TimeEnd(string label = null)
    {
        InternalTimeLog(label, end: true);
    }

    public void TimeLog(string label = null)
    {
        InternalTimeLog(label, end: false);
    }

    private void InternalTimeLog(string label, bool end)
    {
        label ??= "default";

        if (!timers.TryGetValue(label, out var started))
        {
            Warn($"Timer '{label}' does not exist.");
            return;
        }

        var elapsed = Stopwatch.GetTimestamp() - started;
        string ms = (elapsed / 10000d).ToString(CultureInfo.InvariantCulture);
        string message = $"{label}: {ms} ms";
        if (end)
        {
            message += " - timer ended.";
            timers.Remove(label);
        }
        Log(message);
    }

    public void Trace()
    {
        // TODO: Stack trace from console.trace()
    }

    public void Warn(params JsValue[] values)
    {
        Log(LogType.Warn, values);
    }

    private void EnsureOnEngineThread()
    {
        System.Diagnostics.Debug.Assert(Environment.CurrentManagedThreadId == engineThreadId,
            "Console methods should only be called on engine thread");
    }
}