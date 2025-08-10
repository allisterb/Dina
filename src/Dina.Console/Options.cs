namespace Dina.Console;

using CommandLine;
using CommandLine.Text;

public class Options
{
    [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
    public bool Debug { get; set; }

    [Option("no-beeper", Required = false, HelpText = "Disable beeper.")]
    public bool NoBeeper { get; set; }

    [Option("mupdf", Required = false, Default = false, HelpText = "Path to the MuPdf tool.")]
    public string? MuPdfPath { get; set; } = null;

    [Option("tesseract", Required = false, Default = false, HelpText = "Path to the Tesseract OCR tool.")]
    public string? TesseractPath { get; set; } = null;

    [Option("home-dir", Required = false, Default = false, HelpText = "Path to the user's data directory.")]
    public string? HomeDir { get; set; } = null;

    [Option("kb-dir", Required = false, Default = false, HelpText = "Path to the user's knowledge base directory.")]
    public string? KBDir { get; set; } = null;
}





