namespace Internal.ReadLine.Abstractions
{
    internal interface IConsole
    {
        int CursorLeft { get; }
        int CursorTop { get; }
        int BufferWidth { get; }
        int BufferHeight { get; }
        void SetCursorPosition(int left, int top);
#if WINDOWS
        void SetBufferSize(int width, int height);
#endif
        void Write(string value);
        void WriteLine(string value);
    }
}