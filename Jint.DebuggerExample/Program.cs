using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JintDebuggerExample.Helpers;

namespace JintDebuggerExample;

internal class Program
{
    private class Options
    {
        public string FileName { get; }

        public Options(string fileName)
        {
            FileName = fileName;
        }
    }

    private static string Version => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.0";

    private static void Main(string[] args)
    {
        Console.WriteLine(ConsoleHelpers.Color($"Simple Jint Example Debugger v{Version}", 0xffcc00));
        Console.WriteLine();
        try
        {
            var options = ParseArguments(args);

            var debugger = new Debugger();
            debugger.Execute(options.FileName);
        }
        catch (ProgramException ex)
        {
            OutputError(ex);
        }
    }

    private static Options ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ProgramException("No script specified.");
        }

        return new Options(args[0]);
    }

    private static void OutputError(Exception ex)
    {
        Console.WriteLine(ex.Message);
        OutputHelp();
    }

    private static void OutputHelp()
    {
        string exeName = Path.GetFileName(Assembly.GetExecutingAssembly().FullName) ?? "JintDebuggerExample";
        Console.WriteLine($"Usage: {exeName} <path to script>");
    }
}