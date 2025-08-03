namespace Dina.Console;

using Spectre.Console;


internal class Program : Runtime
{
    #region Enums
    public enum ExitResult
    {
        SUCCESS = 0,
        UNHANDLED_EXCEPTION = 1,
        INVALID_OPTIONS = 2,
        NOT_FOUND = 4,
        INVALID_INPUT = 5,
        UNKNOWN_ERROR = 7
    }
    #endregion

    static void Main(string[] args)
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.InputEncoding = System.Text.Encoding.UTF8;
        System.Console.BufferWidth = System.Console.LargestWindowWidth;
        AnsiConsole.Clear();
        AnsiConsole.Write(new Align(new FigletText("Dina").Centered().Color(Color.Yellow), HorizontalAlignment.Center, VerticalAlignment.Top));
        Controller.Start();
    }

    public static void Exit(ExitResult result)
    {
        if (Cts != null && !Cts.Token.CanBeCanceled)
        {
            Cts.Cancel();
            Cts.Dispose();
        }
        Environment.Exit((int)result);
    }
}

