namespace Dina.Console;

using Spectre.Console;
internal class Program : Runtime
{
    static void Main(string[] args)
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.InputEncoding = System.Text.Encoding.UTF8;
        AnsiConsole.Write(new FigletText("Dina").Centered().Color(Color.Yellow));
        Controller.Start();
    }
}

