using System;
using Spectre.Console;
namespace Internal.ReadLine.Abstractions
{
    internal class Console2 : IConsole
    {
        public int CursorLeft => Console.CursorLeft;

        public int CursorTop => Console.CursorTop;

        public int BufferWidth => Console.BufferWidth;

        public int BufferHeight => Console.BufferHeight;

        public bool PasswordMode { get; set; }

#if WINDOWS
        public void SetBufferSize(int width, int height) => Console.SetBufferSize(width, height);
#endif
        public void SetCursorPosition(int left, int top)
        {
            if (!PasswordMode)
                Console.SetCursorPosition(left, top);
        }

        public void Write(string value)
        {
            if (PasswordMode)
                value = new String(default(char), value.Length);

            AnsiConsole.Markup(value);
        }

        public void WriteLine(string value) => AnsiConsole.MarkupLine(value);
    }
}