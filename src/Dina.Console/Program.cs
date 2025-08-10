namespace Dina.Console;

using CommandLine;
using CommandLine.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using Spectre.Console;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
        config = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: false)
           .Build();
        Documents.MuPdfPath = config["Programs:MuPdf"] ?? Documents.MuPdfPath;
        Documents.TesseractPath = config["Programs:Tesseract"] ?? Documents.TesseractPath;
        Documents.HomeDir = config["Files:HomeDir"] ?? Documents.HomeDir;
        Documents.KBDir = config["Files:KBDir"] ?? Documents.KBDir;
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
            Documents.MuPdfPath = o.MuPdfPath ?? Documents.MuPdfPath;
            Documents.TesseractPath = o.TesseractPath ?? Documents.TesseractPath;   
            Documents.HomeDir = o.HomeDir ?? Documents.HomeDir;
            Documents.KBDir = o.KBDir ?? Documents.KBDir;

            if (Directory.Exists(Documents.MuPdfPath))
            {
                if (!File.Exists(Documents.MuPdfToolPath))
                {
                    Error("MuPdf not found at the specified path: {path}", Documents.MuPdfToolPath);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
            }
            else
            {
                Error("MuPdf path does not exist: {path}", Documents.MuPdfPath);
                Exit(ExitResult.INVALID_OPTIONS);
            }


            if (Directory.Exists(Documents.TesseractPath))
            {
                if (!File.Exists(Documents.TesseractToolPath))
                {
                    Error("Tesseract not found at the specified path: {path}", Documents.TesseractToolPath);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
            }
            else
            {
                Error("Tesseract path does not exist: {path}", Documents.TesseractPath);
                Exit(ExitResult.INVALID_OPTIONS);
            }

            if (!Directory.Exists(Documents.HomeDir))
            {
                Error("User home directory does not exist: {path}", Documents.HomeDir);
                Exit(ExitResult.INVALID_OPTIONS);
            }

            if (!Directory.Exists(Documents.KBDir))
            {
                Error("Knowledge base directory does not exist: {path}", Documents.KBDir);
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
        help.Heading = new HeadingInfo("Lokad.Onnx command-line help");
        help.Copyright = "";
        if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
        {
            help.Heading = new HeadingInfo("Lokad.Onnx", AssemblyVersion.ToString(3));
            help.Copyright = "";
            Info(help);
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
            Info(help.ToString().Replace("--", ""));
            Exit(ExitResult.SUCCESS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError))
        {
            HelpRequestedError error = (HelpRequestedError)errors.First(e => e.Tag == ErrorType.HelpRequestedError);
            help.AddVerbs(result.TypeInfo.Current);
            help.AddOptions(result);
            help.AddPreOptionsLine($"{result.TypeInfo.Current.Name.Replace("Options", "").ToLower()} options:");
            Info(help);
            Exit(ExitResult.SUCCESS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
        {
            help.AddVerbs(optionTypes);
            Info(help);
            Exit(ExitResult.INVALID_OPTIONS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.MissingRequiredOptionError))
        {
            MissingRequiredOptionError error = (MissingRequiredOptionError)errors.First(e => e.Tag == ErrorType.MissingRequiredOptionError);
            Info(help);
            Error("A required option is missing.");

            Exit(ExitResult.INVALID_OPTIONS);
        }
        else if (errors.Any(e => e.Tag == ErrorType.UnknownOptionError))
        {
            UnknownOptionError error = (UnknownOptionError)errors.First(e => e.Tag == ErrorType.UnknownOptionError);
            help.AddVerbs(optionTypes);
            Info(help);
            Error("Unknown option: {error}.", error.Token);
            Exit(ExitResult.INVALID_OPTIONS);
        }
        else
        {
            Error("An error occurred parsing the program options: {errors}.", errors);
            help.AddVerbs(optionTypes);
            Info(help);
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
    static Type[] optionTypes = { typeof(Options) };

    static IConfigurationRoot config;

    static string? MuPdfPath;

    static string? TesseractPath;

    static string? HomeDir;

    static string? KBDir;

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

