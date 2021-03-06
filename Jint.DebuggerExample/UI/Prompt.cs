using Jint.DebuggerExample.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jint.DebuggerExample.UI
{
    public class Prompt : DisplayArea
    {
        private bool running;
        private string prompt = "debug>";

        public event Action<string> Command;

        public Prompt(Display display, Bounds bounds) : base(display, bounds)
        {
        }

        public void Start()
        {
            Task.Run(Input);
        }

        public void Stop()
        {
            running = false;
        }

        public override void Redraw()
        {
            // Note that when screen is redrawn, it will be cleared, including anything
            // typed in the prompt (ReadLine). The content of the prompt will still be remembered,
            // however, which means anything typed after redrawing will be appended to what was
            // there before when pressing ENTER, and it will be possible to backspace over the "prompt>" text.
            // There's no easy way around this. For now, don't resize mid-typing.
            display.DrawText(prompt, bounds);
            display.MoveCursor(prompt.Length + 1, bounds.Top.ToAbsolute(display.Rows));
        }

        private void Input()
        {
            running = true;
            while (running)
            {
                Invalidate();
                // Yeah, ReadLine is blocking, so can't cancel this thread if it's pending.
                // But we get a bit lucky here - when we cancel the thread due to an "exit" command,
                // we've just left the ReadLine call.
                string commandLine = Console.ReadLine();
                if (Command != null)
                {
                    Dispatcher.Invoke(() => Command(commandLine));
                }
            }
        }
    }
}