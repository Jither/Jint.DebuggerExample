using System;
using System.Collections.Generic;
using Esprima;
using Jint.DebuggerExample;
using Jint.Native;
using JintDebuggerExample.Helpers;

namespace JintDebuggerExample;

/// <summary>
/// Handles debugger command line parsing, output and rendering.
/// </summary>
internal class CommandLine
{
    /// <summary>
    /// Callback and metadata (for "help" generation) for a single debugger command
    /// </summary>
    private class CommandHandler
    {
        public Func<string, bool> Callback { get; }
        public string Description { get; }
        public string Command { get; }
        public string? ShortCommand { get; }
        public string? Parameters { get; }

        public CommandHandler(string description, Func<string, bool> callback, string command, string? shortCommand = null, string? parameters = null)
        {
            Description = description;
            Callback = callback;
            Command = command;
            ShortCommand = shortCommand;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return $"{Command,-10} {ShortCommand,-5} {Parameters,-20} {Description,-40}";
        }
    }

    private readonly Dictionary<string, CommandHandler> commandHandlersByCommand = new();
    private readonly List<CommandHandler> commandHandlers = new();
    private readonly ValueRenderer renderer = new();

    /// <summary>
    /// The input loop. Keeps asking for a command until a valid command is entered, and it's handler returns
    /// true (handlers that continue execution).
    /// </summary>
    public void Input()
    {
        bool done;
        do
        {
            Console.Write(ConsoleHelpers.Color("> ", 0x88bbff));
            string? commandLine = Console.ReadLine();

            if (commandLine == null)
            {
                break;
            }

            done = HandleCommand(commandLine);
        }
        while (!done);
    }

    /// <summary>
    /// Registers a debugger command. This allows us to auto-generate the help.
    /// </summary>
    public void Register(string description, Func<string, bool> callback, string command, string? shortCommand = null, string? parameters = null)
    {
        if (commandHandlersByCommand.TryGetValue(command, out var existingHandler))
        {
            throw new ArgumentException($"Command name '{command}' is already in use by {existingHandler}");
        }

        var handler = new CommandHandler(description, callback, command, shortCommand, parameters);
        commandHandlers.Add(handler);
        commandHandlersByCommand.Add(command, handler);
        if (shortCommand != null)
        {
            if (commandHandlersByCommand.TryGetValue(shortCommand, out existingHandler))
            {
                throw new ArgumentException($"Short command name '{command}' is already in use by {existingHandler}");
            }
            commandHandlersByCommand.Add(shortCommand, handler);
        }
    }

    public void Output(string message)
    {
        Console.WriteLine(message);
    }

    public void OutputHelp()
    {
        foreach (var handler in commandHandlers)
        {
            Console.WriteLine(handler);
        }
    }

    /// <summary>
    /// Outputs a nice information line about the current position in the script.
    /// </summary>
    public void OutputPosition(Location location, string line)
    {
        var pos = location.Start;
        // Insert colored position marker:
        line = String.Concat(
            line.AsSpan(0, pos.Column),
            ConsoleHelpers.Color("»", 0x88cc55),
            line.AsSpan(pos.Column));

        var locationString = ConsoleHelpers.Color($"{location.Source?.CropStart(20)} {pos.Line,4}:{pos.Column,4}", 0x909090);

        Output($"{locationString}  {line}");
    }

    /// <summary>
    /// Outputs a single binding or object property (name + value)
    /// </summary>
    public void OutputBinding(string name, JsValue value)
    {
        Output(renderer.RenderBinding(name, value));
    }

    /// <summary>
    /// Outputs a single JsValue.
    /// </summary>
    public void OutputValue(JsValue value)
    {
        string valueString = renderer.RenderValue(value, renderProperties: true);
        Output(valueString);
    }

    private bool HandleCommand(string commandLine)
    {
        // Split into command name and arguments (arguments as a single string)
        var parts = commandLine.Split(" ", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            // Nothing entered
            return false;
        }

        var command = parts[0];

        try
        {
            if (!commandHandlersByCommand.TryGetValue(command, out var commandHandler))
            {
                throw new CommandException($"Unknown command: {command}");
            }

            var arguments = parts.Length == 2 ? parts[1] : String.Empty;

            return commandHandler.Callback(arguments);
        }
        catch (CommandException ex)
        {
            Output(ConsoleHelpers.Color(ex.Message, 0xdd6666));
            return false;
        }
    }

    // Parse* - Simple argument parser methods for the debugger commands.

    public Position ParseBreakPoint(string args)
    {
        if (args == String.Empty)
        {
            throw new CommandException("You need to specify a breakpoint position, e.g. 'break 5' or 'break 5:4'");
        }
        var parts = args.Split(":");
        if (!Int32.TryParse(parts[0], out int line))
        {
            throw new CommandException("Breakpoint line should be an integer");
        }

        int column = 0;
        if (parts.Length == 2)
        {
            if (!Int32.TryParse(parts[1], out column))
            {
                throw new CommandException("Breakpoint column should be an integer");
            }
        }

        return new Position(line, column);
    }

    public int ParseIndex(string args, int count)
    {
        if (args == String.Empty)
        {
            throw new CommandException("You need to specify an index");
        }
        if (!Int32.TryParse(args, out int index))
        {
            throw new CommandException("Index must be an integer");
        }

        if (index < 0 || index >= count)
        {
            string range = count switch
            {
                < 1 => "no entries in list",
                  1 => "0",
                > 1 => $"0 - {count}"
            };
            throw new CommandException($"Index {index} out of range ({range})");
        }

        return index;
    }
}
