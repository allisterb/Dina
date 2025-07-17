namespace Dina.Console;

using Spectre.Console;
internal class Program : Runtime
{
    static void Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Dina").Centered().Color(Color.Yellow));
        Controller.Start();
    }
}

