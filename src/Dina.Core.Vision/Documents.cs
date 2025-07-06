namespace Dina;

using System;
using System.IO;
using static Dina.Result;

public class Documents : Runtime
{
    public static string MuPdfPath { get; set; } = "bin";

    public static string MuPdfToolPath => Path.Combine(MuPdfPath, "mutool");

    public static Result<byte[][]> ConvertPdfToImages(string pdfFilePath, string outputDirectory)
    {
        FailIfFileDoesNotExist(pdfFilePath);
        CreateIfDirectoryDoesNotExist(outputDirectory);
        var name = RandomString(10);
        var r = RunCmd(MuPdfToolPath, $"convert -o {outputDirectory}\\{name}-%d.png {pdfFilePath}");
        if (!string.IsNullOrEmpty(r))
        {
            return Result<byte[][]>.Failure($"Failed to convert PDF to images using mutool: {r}");
        }
        var output = Directory.GetFiles(outputDirectory, $"{name}-*.png", SearchOption.TopDirectoryOnly);
        if (output is null)
        {
            return Result<byte[][]>.Failure("Failed to convert PDF to images using mutool: could not find generated images.");
        }
        else
        {
            var result = output.Select(f => File.ReadAllBytes(f)).ToArray();
            foreach (var f in output)
            {
                File.Delete(f);
            }
            return Result<byte[][]>.Success(result);    
        }
    }

}
