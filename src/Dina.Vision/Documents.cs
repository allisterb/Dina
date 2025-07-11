namespace Dina;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NAPS2.Images;
using NAPS2.Images.ImageSharp;
using NAPS2.Scan;
using OpenCvSharp;

using static Dina.Result;

public class Documents : Runtime
{
    public static string MuPdfPath { get; set; } = "bin";

    public static string MuPdfToolPath => Path.Combine(MuPdfPath, "mutool");

    public static Result<byte[][]> ConvertPdfToImages(string pdfFilePath, string? outputDirectory = null)
    {
        if (!File.Exists(pdfFilePath)) return FileDoesNotExistFailure<byte[][]>(pdfFilePath);
        outputDirectory ??= Path.Combine(AssemblyLocation, "convertpdf");
        CreateIfDirectoryDoesNotExist(outputDirectory);
        var name = RandomString(10);
        var r = RunCmd(MuPdfToolPath, $"convert -o {outputDirectory}\\{name}-%d.png {pdfFilePath}");
        if (!(r.IsSuccess && r.Value == ""))
        {
            return Failure<byte[][]> ($"Failed to convert PDF to images using mutool: {r.Message}");
        }
        else if ((r.IsSuccess && r.Value != ""))
        {
            return Failure<byte[][]>($"Failed to convert PDF to images using mutool: {r.Value}");
        }
        var output = Directory.GetFiles(outputDirectory, $"{name}-*.png", SearchOption.TopDirectoryOnly);
        if (output is null || output.Length == 0)
        {
            return Failure<byte[][]>("Failed to convert PDF to images using mutool: could not find generated images.");
        }
        else
        {
            var result = output.Select(f => File.ReadAllBytes(f)).ToArray();
            foreach (var f in output)
            {
                File.Delete(f);
            }
            Info("Converted PDF {0} to {1} images of total size {2} bytes.", pdfFilePath, result.Length, result.Sum(i => i.Length));
            return Success(result);    
        }
    }

    public static Result<string[]> ConvertPdfToText(string pdfFilePath, string? outputDirectory = null)
    {
        if (!File.Exists(pdfFilePath)) return FileDoesNotExistFailure<string[]>(pdfFilePath);
        outputDirectory ??= Path.Combine(AssemblyLocation, "convertpdf");
        CreateIfDirectoryDoesNotExist(outputDirectory);
        var name = RandomString(10);
        var r = RunCmd(MuPdfToolPath, $"convert -F text -o {outputDirectory}\\{name}-%d.txt {pdfFilePath}");
        if (!(r.IsSuccess && r.Value == ""))
        {
            return Failure<string[]>($"Failed to convert PDF to text using mutool: {r.Message}");
        }
        else if ((r.IsSuccess && r.Value != ""))
        {
            return Failure<string[]>($"Failed to convert PDF to text using mutool: {r.Value}");
        }
        var output = Directory.GetFiles(outputDirectory, $"{name}-*.txt", SearchOption.TopDirectoryOnly);
        if (output is null || output.Length == 0)
        {
            return Failure<string[]>("Failed to convert PDF to text using mutool: could not find generated text files.");
        }
        else
        {
            var result = output.Select(f => File.ReadAllText(f)).ToArray();
            foreach (var f in output)
            {
                File.Delete(f);
            }
            Info("Converted PDF {0} to {1} strings of total size {2} characters.", pdfFilePath, result.Length, result.Sum(i => i.Length));
            return Success(result);
        }
    }

    public static async Task<string[]> GetScannerNames()
    {
        using var scanningContext = new ScanningContext(new ImageSharpImageContext());
        var controller = new ScanController(scanningContext);
        var devices = await controller.GetDeviceList();
        return devices?.Select(d => d.Name).ToArray() ?? Array.Empty<string>();
        
    }
    public static async Task<byte[][]> Scan()
    {
        using ScanningContext scanningContext = new ScanningContext(new ImageSharpImageContext());
        var controller = new ScanController(scanningContext);
        var device = (await controller.GetDeviceList()).First();
        var op = Begin("Using scanner {0}", device.Name); 
        var options = new ScanOptions
        {
            Device = device,
        };
        var i = 0;
        List<byte[]> output = new List<byte[]>();    
        await foreach (var image in controller.Scan(options))
        {
            output.Add(image.SaveToMemoryStream(ImageFileFormat.Jpeg).ToArray());
            i++;
        }
        op.Complete();
        Info("Scanned {0} images from scanner {1}.", i, device.Name);   
        return output.ToArray();
    }
}


    

   
