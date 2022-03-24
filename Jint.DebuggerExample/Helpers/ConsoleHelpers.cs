using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JintDebuggerExample.Helpers;

internal static class ConsoleHelpers
{
    private static bool IsColorEnabled => !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") == null;

    public static string Color(string str, uint color)
    {
        if (!IsColorEnabled)
        {
            return str;
        }
        uint red = (color >> 16) & 0xff;
        uint green = (color >> 8) & 0xff;
        uint blue = color & 0xff;

        int returnToConsoleColor = ConsoleColorToAnsi(Console.ForegroundColor);
        return $"\x1b[38;2;{red};{green};{blue}m{str}\x1b[38;5;{returnToConsoleColor}m";
    }

    private static int ConsoleColorToAnsi(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => 0,
            ConsoleColor.DarkRed => 1,
            ConsoleColor.DarkGreen => 2,
            ConsoleColor.DarkYellow => 3,
            ConsoleColor.DarkBlue => 4,
            ConsoleColor.DarkMagenta => 5,
            ConsoleColor.DarkCyan => 6,
            ConsoleColor.Gray => 7,
            ConsoleColor.DarkGray => 8,
            ConsoleColor.Red => 9,
            ConsoleColor.Green => 10,
            ConsoleColor.Yellow => 11,
            ConsoleColor.Blue => 12,
            ConsoleColor.Magenta => 13,
            ConsoleColor.Cyan => 14,
            ConsoleColor.White => 15,
            _ => throw new ArgumentException($"Unknown ConsoleColor: {color}"),
        };
    }
}
