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
        public bool IsModule { get; }
        public string Path { get; }

        public Options(string path, bool isModule)
        {
            Path = path;
            IsModule = isModule;
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

            var basePath = Path.GetDirectoryName(options.Path);
            if (basePath == null)
            {
                throw new ProgramException("Couldn't determine base path for script.");
            }

            var debugger = new Debugger(basePath, options.IsModule);
            debugger.Execute(options.Path);
        }
        catch (ProgramException ex)
        {
            OutputError(ex);
        }
    }

    private static Options ParseArguments(string[] args)
    {
        string? path = null;
        bool isModule = false;
        foreach (var arg in args)
        {
            if (arg.StartsWith('-'))
            {
                switch (arg[1..])
                {
                    case "m":
                        isModule = true;
                        break;
                    default:
                        throw new ProgramException($"Unknown option: {arg}");
                }
            }
            else
            {
                path = Path.GetFullPath(arg);
            }
        }

        if (path == null)
        {
            throw new ProgramException("No script/module path specified.");
        }

        return new Options(path, isModule);
    }

    private static void OutputError(Exception ex)
    {
        Console.WriteLine(ex.Message);
        OutputHelp();
    }

    private static void OutputHelp()
    {
        string exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location) ?? "JintDebuggerExample";
        Console.WriteLine($"Usage: {exeName} <path to script> [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -m   Load script as module.");
    }
}