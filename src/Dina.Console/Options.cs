namespace Dina.Console;

using CommandLine;
using CommandLine.Text;

[Verb("run", HelpText = "Run the Dina console application with specified options.")]
public class Options
{
    [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
    public bool Debug { get; set; }

    [Option("no-beeper", Required = false, HelpText = "Disable beeper.")]
    public bool NoBeeper { get; set; }

    [Option("braille", Required = false, Default = null, HelpText = "Simulate braille output.")]
    public bool? SimulateBraille { get; set; }

    [Option("mupdf", Required = false, Default = null, HelpText = "Path to the MuPdf tool.")]
    public string? MuPdfPath { get; set; }

    [Option("tesseract", Required = false, Default = null, HelpText = "Path to the Tesseract OCR tool.")]
    public string? TesseractPath { get; set; }

    [Option("home-dir", Required = false, Default = null, HelpText = "Path to the user's data directory.")]
    public string? HomeDir { get; set; }

    [Option("kb-dir", Required = false, Default = null, HelpText = "Path to the user's knowledge base directory.")]
    public string? KBDir { get; set; }
}





