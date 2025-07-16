namespace Dina.Console;

using CommandLine;
using CommandLine.Text;

public class Options
{
    [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
    public bool Debug { get; set; }

    [Option('b', "no-beeper", Required = false, Default = false, HelpText = "Disable the beeper sound.")]
    public bool NoBeeper { get; set; }

}





