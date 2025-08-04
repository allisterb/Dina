namespace Dina.Console;

using KokoroSharp;
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
        if (!KokoroTTS.IsDownloaded(KModel.float32))
        {
            DownloadKokoroModel();
        }
        if (!KokoroTTS.IsDownloaded(KModel.float32))
        {
            AnsiConsole.MarkupLine("[red] Could not download Kokoro TTS model. Exiting.");
            Exit(ExitResult.UNKNOWN_ERROR);
        }
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
        var ex = (Exception) e.ExceptionObject;
        AnsiConsole.WriteLine($"[red] {ex.Message}[/]");
        if (ex.InnerException is not null)
        {
            AnsiConsole.WriteLine($"[red] Inner Exception: {ex.InnerException.Message}[/]");
        }
        ResetConsoleColors();
        Environment.Exit((int)ExitResult.UNHANDLED_EXCEPTION);
    }

    public static ConsoleColor fgcolor = System.Console.ForegroundColor;

    public static ConsoleColor bgcolor = System.Console.BackgroundColor;

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
            KokoroTTS.LoadModelAsync(KModel.float32, (p) => t.Increment(p)).Wait();
            t.StopTask();
        });

    }
}

