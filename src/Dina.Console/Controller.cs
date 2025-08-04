namespace Dina.Console;

using System;
using System.Drawing;
using System.Text;

using Spectre.Console;

using SystemColor = System.Drawing.Color;

using Dina.Speech;
using static Program;

internal class Controller
{
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
        Prompt();
    }


    internal static void Prompt()
    {
        Console.WriteLine(TextToBraille("Hello this is Dina, your AI assistant. Type 'help' for commands.\n"), SystemColor.Yellow);
    loop:
        TTS.StartTTSWorker();
        inputEnabled = true;
        string input = ReadLine.Read(promptString, KeyProc);         
        AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(true)
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
            if (activeConversation is null)
            {
                activeConversation = new AgentConversation("You are a helpful agent");
            }
            StringBuilder s = new StringBuilder();
            await foreach (var response in activeConversation.Prompt(input))
            {
                if (string.IsNullOrEmpty(response.Content))
                    continue;
                else if (string.IsNullOrEmpty(response.Content.Trim()))
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
                else if (response.Content.Contains("\n"))
                {
                    if (!t.IsFinished) t.StopTask();
                    if (response.Content.EndsWith("\n"))
                    {
                        s.Append(response.Content);
                        SayInfoLine(s.ToString());
                        s.Clear();
                    }
                    else
                    {
                        var chunks = response.Content.Split("\n");
                        s.Append(chunks[0]);
                        SayInfoLine(s.ToString());
                        s.Clear();
                        foreach (var c in chunks.Take(1..)) s.AppendLine(c.Trim('\n'));
                    }
                }
                else
                {
                    s.Append(response.Content);
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
            SayErrorLine(ex.Message);
            if (ex.InnerException is not null)
            {
                SayErrorLine("Inner Exception: {0}", ex.InnerException.Message);
            }
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
        var text = string.Format(template, args);
        TTS.EnqueueTTS(text);
        //tts.SpeakFast(text, voice, new KokoroSharp.Processing.KokoroTTSPipelineConfig() { Speed = 3.0f});
        //tts.SpeakFast(text, voice);
        AnsiConsole.MarkupLine($"[lightgoldenrod2_1]{template}[/]", args);
        AnsiConsole.MarkupLine($"[yellow]{TextToBraille(text)}[/]");
       
    }

    internal static void SayErrorLine(string template, params object[] args)
    {
        AnsiConsole.MarkupLine($"[red]{template}[/]", args);
        AnsiConsole.MarkupLine($"[red]{TextToBraille(string.Format(template, args))}[/]");
        AnsiConsole.ResetColors();
    }

    // Translate from https://github.com/vineethsubbaraya/pybraille/blob/main/pybraille/main.py
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

    static ModelConversation? activeConversation;
    #endregion
}

