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
        fgcolor = System.Console.ForegroundColor;
        bgcolor = System.Console.BackgroundColor;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.InputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Dina").Centered().Color(Color.Yellow));
        Controller.Start();
    }

    public static void Exit(ExitResult result)
    {
        if (Cts != null && !Cts.Token.CanBeCanceled)
        {
            Cts.Cancel();
            Cts.Dispose();
        }
        ResetConsoleColors();
        Environment.Exit((int)result);
    }

    public static void ResetConsoleColors()
    {
        System.Console.ForegroundColor = fgcolor;
        System.Console.BackgroundColor = bgcolor;   
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        //AnsiConsole.WriteLine()
        ResetConsoleColors();
    }

    public static ConsoleColor fgcolor = System.Console.ForegroundColor;

    public static ConsoleColor bgcolor = System.Console.BackgroundColor;
}

