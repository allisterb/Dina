namespace Dina.Console;

using Microsoft.Extensions.Configuration;

using CommandLine;
using CommandLine.Text;
using Serilog;
using Serilog.Extensions.Logging;
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

    static Program()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        var logger = new LoggerConfiguration()
           .Enrich.FromLogContext()
           .MinimumLevel.Verbose()
           .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.CLI.log"))
           .CreateLogger();
        var lf = new SerilogLoggerFactory(logger);
        var lp = new SerilogLoggerProvider(logger, false);
        Initialize("Dina", "CLI", true, lf, lp);
        if (File.Exists("testappsettings.json"))
        {
            config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: false)
               .Build();
            Documents.muPdfPath = config["Programs:MuPdf"] ?? Documents.muPdfPath;
            Documents.tesseractPath = config["Programs:Tesseract"] ?? Documents.tesseractPath;
            Documents.homeDir = config["Files:HomeDir"] ?? Documents.homeDir;
            Documents.kbDir = config["Files:KBDir"] ?? Documents.kbDir;
            Documents.config = config;
            ModelConversation.config = config;
            Memory.config = config;
            simulateBraille = bool.Parse(config?["Console:SimulateBraille"] ?? "False");
        }
    }

    #region Methods

    #region Entry-point
    static void Main(string[] args)
    {
        fgcolor = System.Console.ForegroundColor;
        bgcolor = System.Console.BackgroundColor;
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.InputEncoding = System.Text.Encoding.UTF8;
        var result = new Parser().ParseArguments(args, optionTypes);
        result
        .WithParsed<Options>(o =>
        {
            Documents.muPdfPath = o.MuPdfPath ?? Documents.muPdfPath;
            Documents.tesseractPath = o.TesseractPath ?? Documents.tesseractPath;   
            Documents.homeDir = o.HomeDir ?? Documents.homeDir;
            Documents.kbDir = o.KBDir ?? Documents.kbDir;
            simulateBraille = o.SimulateBraille ?? simulateBraille;
            if (Directory.Exists(Documents.muPdfPath))
            {
                if (!File.Exists(Documents.MuPdfToolPath))
                {
                    ErrorLine("mutool not found at the path: {0}", Documents.MuPdfToolPath);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
            }
            else
            {
                ErrorLine("MuPdf directory does not exist: {0}", Documents.muPdfPath);
                Exit(ExitResult.INVALID_OPTIONS);
            }


            if (Directory.Exists(Documents.tesseractPath))
            {
                if (!File.Exists(Documents.TesseractToolPath))
                {
                    ErrorLine("Tesseract not found at the specified path: {path}", Documents.TesseractToolPath);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
            }
            else
            {
                ErrorLine("Tesseract path does not exist: {path}", Documents.tesseractPath);
                Exit(ExitResult.INVALID_OPTIONS);
            }

            if (!Directory.Exists(Documents.homeDir))
            {
                ErrorLine("User home directory does not exist: {path}", Documents.homeDir);
                Exit(ExitResult.INVALID_OPTIONS);
            }

            if (!Directory.Exists(Documents.kbDir))
            {
                ErrorLine("Knowledge base directory does not exist: {path}", Documents.kbDir);
                Exit(ExitResult.INVALID_OPTIONS);
            }

            AnsiConsole.Clear();
            Controller.Start();
        })
        .WithNotParsed(errors => Help(result, errors));
    }
    #endregion

    static void Help(ParserResult<object> result, IEnumerable<Error> errors)
    {
        HelpText help = GetAutoBuiltHelpText(result);
        help.Heading = new HeadingInfo("Dina command-line help");
        help.Copyright = "";
        if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
        {
            help.Heading = new HeadingInfo("Dina", AssemblyVersion.ToString(3));
            help.Copyright = "";
            InfoLine(help);
            Exit(ExitResult.SUCCESS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.HelpVerbRequestedError))
        {
            HelpVerbRequestedError error = (HelpVerbRequestedError)errors.First(e => e.Tag == ErrorType.HelpVerbRequestedError);
            if (error.Type != null)
            {
                help.AddVerbs(error.Type);
            }
            else
            {
                help.AddVerbs(optionTypes);
            }
            InfoLine(help.ToString().Replace("--", ""));
            Exit(ExitResult.SUCCESS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError))
        {
            HelpRequestedError error = (HelpRequestedError)errors.First(e => e.Tag == ErrorType.HelpRequestedError);
            help.AddVerbs(result.TypeInfo.Current);
            help.AddOptions(result);
            help.AddPreOptionsLine($"{result.TypeInfo.Current.Name.Replace("Options", "").ToLower()} options:");
            InfoLine(help);
            Exit(ExitResult.SUCCESS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
        {
            help.AddVerbs(optionTypes);
            InfoLine(help);
            Exit(ExitResult.INVALID_OPTIONS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.MissingRequiredOptionError))
        {
            MissingRequiredOptionError error = (MissingRequiredOptionError)errors.First(e => e.Tag == ErrorType.MissingRequiredOptionError);
            InfoLine(help);
            ErrorLine("A required option is missing.");

            Exit(ExitResult.INVALID_OPTIONS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.UnknownOptionError))
        {
            UnknownOptionError error = (UnknownOptionError)errors.First(e => e.Tag == ErrorType.UnknownOptionError);
            help.AddVerbs(optionTypes);
            InfoLine(help);
            ErrorLine("Unknown option: {error}.", error.Token);
            Exit(ExitResult.INVALID_OPTIONS);
        }
        else
        {
            ErrorLine("An error occurred parsing the program options: {errors}.", errors);
            help.AddVerbs(optionTypes);
            InfoLine(help);
            Exit(ExitResult.INVALID_OPTIONS);
        }
    }

    static HelpText GetAutoBuiltHelpText(ParserResult<object> result)
    {
        return HelpText.AutoBuild(result, h =>
        {
            h.AddOptions(result);
            HelpText.DefaultParsingErrorsHandler(result, h);
            return h;
        },
        e => e);
    }

    static void InfoLine(string template, params object[] args)
    {
        Info(template, args);
        var text = Markup.Escape(args.Length == 0 ? template : string.Format(template, args));
        AnsiConsole.MarkupLine($"[lightgoldenrod2_1]{text}[/]");
    }

    static void ErrorLine(string template, params object[] args)
    {
        Error(template, args);  
        var text = Markup.Escape(args.Length == 0 ? template : string.Format(template, args));
        AnsiConsole.MarkupLine($"[red]{text}[/]");
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
    #endregion

    #region Fields
    public readonly static IConfigurationRoot? config;

    internal static bool simulateBraille = false;

    static Type[] optionTypes = { typeof(Options) };

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

