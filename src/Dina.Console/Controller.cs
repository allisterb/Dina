namespace Dina.Console;

using System;
using System.Drawing;
using System.Text;

using Spectre.Console;

using static Program;

internal class Controller
{
    #region Constructor
    static Controller()
    {
        ModelRuntime modelRuntime = ModelRuntime.Ollama;
        string textModel = OllamaModels.Gemma3n_e4b_tools_test; 
        string embeddingModel = OllamaModels.Nomic_Embed_Text;
        string endpointUrl = "http://localhost:11434";  
        if (config is not null && config["Model:Runtime"] is not null)
        {
            modelRuntime = Program.config["Model:Runtime"]?.ToLower() switch
            {
                "llamacpp" => ModelRuntime.LlamaCpp,
                "openai" => ModelRuntime.OpenAI,
                _ => modelRuntime,
            };
            textModel = config["Model:TextModel"] ?? textModel;
            embeddingModel = config["Model:EmbeddingModel"] ?? embeddingModel;
            endpointUrl = config["Model:EndpointUrl"] ?? endpointUrl;

        }
        agentManager = new AgentManager(modelRuntime, textModel, embeddingModel, endpointUrl);
        activeConversation = agentManager.StartUserSession();   
    }
    #endregion

    #region Methods
    internal static void EnableBeeper()
    {
        if (options.NoBeeper) return;
        beeperThread.Name = "Beeper";
        beeperThread.IsBackground = true;
        beeperThread.Start();
    }
    internal static void StartBeeper()
    {
        if (options.NoBeeper) return;
        signalBeep.Set();
        beeperOn = true;
    }

    internal static void StopBeeper()
    {
        if (options.NoBeeper) return;
        signalBeep.Reset();
        beeperOn = false;
    }

    internal static void SetPrompt(string prompt) => promptString = prompt;

    internal static void SetDefaultPrompt() => promptString = "|>";

    internal static void Start()
    {
        ReadLine.HistoryEnabled = true;
        if (beeperOn) StopBeeper();
        SetDefaultPrompt();
        WaitForTaskToComplete(agentManager.CreateKBAsync(), "Indexing knowledge base", false);
        SayInfoLine("Welcome to Dina. Press F1 or type help at anytime to get help on what you are doing. Press ESC or type quit to quit.");
        Prompt();
    }

    internal static void Prompt()
    {
    loop:
        inputEnabled = true;
        string input = ReadLine.Read(promptString, KeyProc);         
        AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(false)
            .Columns(
            [                
              new SpinnerColumn(Spinner.Known.Dots)
            ])
            .Start(ctx =>
            {
                inputEnabled = false;   
                var t = ctx.AddTask("Thinking..."); 
                HandleInputAsync(t, DateTime.Now, input).Wait();
                t.StopTask();                
            });
    goto loop;
    }
    
    internal static async Task HandleInputAsync(ProgressTask t, DateTime time, string input)
    {
        switch(input)
        {
            case ("$$quit$$"):
                t.StopTask();
                Console.WriteLine("Goodbye!");
                Exit(ExitResult.SUCCESS);
                break;
            default: break;
        }
        try
        {
            StringBuilder s = new StringBuilder();
            await foreach (var response in activeConversation.Prompt(input))
            {
                if (string.IsNullOrEmpty(response.Message.Content))
                    continue;
                else if (string.IsNullOrEmpty(response.Message.Content.Trim()))
                {
                    if (s.Length > 0)
                    {
                        if (!t.IsFinished) t.StopTask();
                        SayInfoLine(s.ToString());
                        s.Clear();
                        continue;
                    }
                    else continue;
                }
                else if (response.Message.Content.Contains("```json") || response.Message.Content.Contains("{\"tool_calls\""))
                {
                    if (!t.IsFinished) t.StopTask();
                    break;
                }
                else if (response.Message.Content.Contains("\n"))
                {
                    if (!t.IsFinished) t.StopTask();
                    if (response.Message.Content.EndsWith("\n"))
                    {
                        s.Append(response.Message.Content);
                        SayInfoLine(s.ToString());
                        s.Clear();
                    }
                    else
                    {
                        var chunks = response.Message.Content.Split("\n");
                        s.Append(chunks[0]);
                        SayInfoLine(s.ToString());
                        s.Clear();
                        foreach (var c in chunks.Take(1..)) s.AppendLine(c.Trim('\n'));
                    }
                }
                else
                {
                    s.Append(response.Message.Content);
                }
            }
            if (!t.IsFinished) t.StopTask();
            if (s.Length > 0)
            {
                SayInfoLine(s.ToString());
                s.Clear();
            }
        }
        catch (Exception ex)
        {
            t.StopTask();
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    internal static string KeyProc(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape: return "$$quit$$";
            case ConsoleKey.F1: return "$$help$$";
            default: return "";
        }
    }

    internal static void SayInfoLine(string template, params object[] args)
    {
        if (template.Length == 0 || (template.Length == 1 && template[0] == '*')) return;
        var text = Markup.Escape(args.Length == 0 ? template : string.Format(template.Replace("{", "").Replace("}", ""), args));
        AnsiConsole.MarkupLine($"[lightgoldenrod2_1]{text}[/]");
        if (simulateBraille) AnsiConsole.MarkupLine($"[yellow]{TextToBraille(text)}[/]");
    }

    internal static void SayErrorLine(string template, params object[] args)
    {
        var text = Markup.Escape(args.Length == 0 ? template : string.Format(template.Replace("{", "").Replace("}", ""), args));
        AnsiConsole.MarkupLine($"[red]{text}[/]");
        if (simulateBraille) AnsiConsole.MarkupLine($"[red]{TextToBraille(text)}[/]");
    }

    // Translated from https://github.com/vineethsubbaraya/pybraille/blob/main/pybraille/main.py
    internal static string TextToBraille(string textToConvert)
    {
        var characterUnicodes = new Dictionary<char, string>
        {
            {'a', "\u2801"}, {'b', "\u2803"}, {'k', "\u2805"}, {'l', "\u2807"},
            {'c', "\u2809"}, {'i', "\u280A"}, {'f', "\u280B"}, {'m', "\u280D"},
            {'s', "\u280E"}, {'p', "\u280F"}, {'e', "\u2811"}, {'h', "\u2813"},
            {'o', "\u2815"}, {'r', "\u2817"}, {'d', "\u2819"}, {'j', "\u281A"},
            {'g', "\u281B"}, {'n', "\u281D"}, {'t', "\u281E"}, {'q', "\u281F"},
            {'u', "\u2825"}, {'v', "\u2827"}, {'x', "\u282D"}, {'z', "\u2835"},
            {'w', "\u283A"}, {'y', "\u283D"}, {'.', "\u2832"}, {'\'', "\u2804"},
            {',', "\u2802"}, {'-', "\u2824"}, {'/', "\u280C"}, {'!', "\u2816"},
            {'?', "\u2826"}, {'$', "\u2832"}, {':', "\u2812"}, {';', "\u2830"},
            {'(', "\u2836"}, {')', "\u2836"}, {' ', " "}, {'1', "\u2801"},
            {'2', "\u2803"}, {'3', "\u2809"}, {'4', "\u2819"}, {'5', "\u2811"},
            {'6', "\u280B"}, {'7', "\u281B"}, {'8', "\u2813"}, {'9', "\u280A"},
            {'0', "\u281A"}
        };
        var numberPunctuations = new HashSet<char> { '.', ',', '-', '/', '$' };
        var escapeCharacters = new HashSet<char> { '\n', '\r', '\t' };
        bool isNumber = false;
        var sb = new StringBuilder();

        foreach (var ch in textToConvert)
        {
            if (escapeCharacters.Contains(ch))
            {
                sb.Append(ch);
                continue;
            }
            char character = ch;
            if (char.IsUpper(character))
            {
                sb.Append("\u2820"); // caps
                character = char.ToLower(character);
            }
            if (char.IsDigit(character))
            {
                if (!isNumber)
                {
                    isNumber = true;
                    sb.Append("\u283C"); // num
                }
            }
            else
            {
                if (isNumber && !numberPunctuations.Contains(character))
                {
                    isNumber = false;
                }
            }
            if (characterUnicodes.TryGetValue(character, out var braille))
            {
                sb.Append(braille);
            }
        }
        return sb.ToString();
    }

    public static void WaitForTaskToComplete(Task task, string? desc = null, bool clear = true )
    {
        ProgressColumn[] columns = desc is not null ? 
            [new SpinnerColumn(Spinner.Known.Dots), new TaskDescriptionColumn()] : 
            [new SpinnerColumn(Spinner.Known.Dots)];
        AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(clear)
            .Columns(columns)
            .Start(ctx =>
            {
                inputEnabled = false;
                var t = ctx.AddTask(desc + "...");
                task.Wait();
                t.StopTask();
            });
    }

    public static T WaitForTaskToComplete<T>(Task<T> task, string? desc = null, bool clear = true)
    {
        ProgressColumn[] columns = desc is not null ?
            [new SpinnerColumn(Spinner.Known.Dots), new TaskDescriptionColumn()] :
            [new SpinnerColumn(Spinner.Known.Dots)];
        AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(clear)
            .Columns(columns)
            .Start(ctx =>
            {
                inputEnabled = false;
                var t = ctx.AddTask(desc + "...");
                task.Wait();
                t.StopTask();
            });
        return task.Result;
    }
    #endregion

    #region Fields

    static Options options = new();

    static ManualResetEvent signalBeep = new ManualResetEvent(false);

    static Thread beeperThread = new Thread(() =>
    {
        while (true)
        {
            signalBeep.WaitOne();
            System.Console.Beep();
            Thread.Sleep(800);
        }
    }, 1);

    static bool beeperOn;

    static string promptString = "|>";

    static string LastPrompt = "";

    static bool inputEnabled = false;

    public static Dictionary<string, string> systemMessage = new Dictionary<string, string>()
            {
                {"quit", "$$quit$$" }
            };

    static AgentManager agentManager;
    
    static AgentConversation activeConversation;
    #endregion
}

