namespace Dina.Console;

using System.Drawing;

using Co = Colorful.Console;

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
        inputEnabled = true;
        string i = ReadLine.Read(promptString);
        HandleInput(DateTime.Now, i);
    }
    
    internal static void HandleInput(DateTime time, string input)
    {
        inputEnabled = false;
        /* 
        if (!string.IsNullOrEmpty(input.Trim()))
        {
            if (!ActivePackage.HandleInput(time, input))
            {
                SayInfoLineIfDebug("Input handled by HOME package.");
                if (!HomePackage.HandleInput(time, input))
                {
                    SayCouldNotUnderstand(input);
                }
            }
        }
        */
        Prompt();
    }

    internal static void SayInfoLine(string template, params object[] args) => Co.WriteLineFormatted(template, Color.Pink, Color.PaleGoldenrod, args);

    internal static void SayErrorLine(string template, params object[] args) => Co.WriteLineFormatted(template, Color.Pink, Color.Red, args);
    #endregion
    #region Fields

    static Options options = new ();

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

    static bool inputEnabled = false;
    #endregion
}

