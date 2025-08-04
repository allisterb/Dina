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

    #region Methods
    static void Main(string[] args)
    {
        fgcolor = System.Console.ForegroundColor;
        bgcolor = System.Console.BackgroundColor;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.InputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.Markup("[green]Loading Dina...[/]");
        AnsiConsole.Clear();
        Controller.Start();
    }

    public static void Exit(ExitResult result)
    {
        if (Cts != null && !Cts.Token.CanBeCanceled)
        {
            Cts.Cancel();
            Cts.Dispose();
        }
        ResetConsole();
        Environment.Exit((int)result);
    }

    static void ResetConsole()
    {
        System.Console.ForegroundColor = fgcolor;
        System.Console.BackgroundColor = bgcolor;
        AnsiConsole.ResetColors();
        AnsiConsole.ResetDecoration();
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception) e.ExceptionObject;
        AnsiConsole.WriteException(ex);
        ResetConsole();
        Environment.Exit((int)ExitResult.UNHANDLED_EXCEPTION);
    }
    #endregion

    #region Fields
    static ConsoleColor fgcolor = System.Console.ForegroundColor;

    static ConsoleColor bgcolor = System.Console.BackgroundColor;
    #endregion

    /*
    internal static void DownloadKokoroModel()
    {
        AnsiConsole.Progress()
        .AutoRefresh(true) // Turn off auto refresh
        .AutoClear(false)   // Do not remove the task list when done
        .HideCompleted(false)   // Hide tasks as they are completed
        .Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),    // Task description
            new PercentageColumn(),         // Percentage
           
        })
        .Start(ctx =>
        {
            var t = ctx.AddTask("Downloading Kokoro TTS model...");
            KokoroTTS.LoadModelAsync(KModel.int8, (p) => t.Increment(p)).Wait();
            t.StopTask();
        });

    }
    */
}

