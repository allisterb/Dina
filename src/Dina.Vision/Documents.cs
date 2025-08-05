namespace Dina;

using Microsoft.Extensions.Logging;
using NAPS2.Images;
using NAPS2.Images.ImageSharp;
using NAPS2.Scan;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dina.Result;

public class Documents : Runtime
{
    public static string BinPath { get; set; } = "bin";

    public static string MuPdfToolPath => Path.Combine(BinPath, "mutool");

    public static string TesseractPath => Path.Combine(BinPath, "tesseract-ocr-5.5.0", "tesseract");
    
    public static Result<byte[][]> ConvertPdfToImages(string pdfFilePath, string? outputDirectory = null, string type="jpg")
    {
        if (!File.Exists(pdfFilePath)) return FileDoesNotExistFailure<byte[][]>(pdfFilePath);
        outputDirectory ??= Path.Combine(AssemblyLocation, "convertpdf");
        CreateIfDirectoryDoesNotExist(outputDirectory);
        var name = RandomString(10);
        var r = RunCmd(MuPdfToolPath, $"convert -o {outputDirectory}\\{name}-%d.{type.ToLower()} {pdfFilePath}");
        if (!(r.IsSuccess && r.Value == ""))
        {
            return Failure<byte[][]> ($"Failed to convert PDF to images using mutool: {r.Message}");
        }
        else if ((r.IsSuccess && r.Value != ""))
        {
            return Failure<byte[][]>($"Failed to convert PDF to images using mutool: {r.Value}");
        }
        var output = Directory.GetFiles(outputDirectory, $"{name}-*.{type.ToLower()}", SearchOption.TopDirectoryOnly);
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
            Info("Converted PDF {0} to {1} image(s) of total size {2} bytes.", pdfFilePath, result.Length, result.Sum(i => i.Length));
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
            Info("Converted PDF {0} to {1} string(s) of total size {2} characters.", pdfFilePath, result.Length, result.Sum(i => i.Length));
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
    public static async Task<Result<byte[][]>> ScanAsync()
    {
        try
        {
            using ScanningContext scanningContext = new ScanningContext(new ImageSharpImageContext());
            var controller = new ScanController(scanningContext);
            var device = (await controller.GetDeviceList()).FirstOrDefault();
            if (device is null)
            {
                return Failure<byte[][]>("No scanner devices found.");
            }
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
            return Success(output.ToArray());
        }
        catch (Exception ex)
        {
            return FailureError<byte[][]>("Failed to scan images.", ex);
        }
    }

    public static byte[] ResizeImage(byte[] imageBytes, int width, int height)
    {
        using var image = Image.Load(imageBytes);
        var format = Image.DetectFormat(imageBytes);
        image.Mutate(x => x.Resize(width, height));
        using var outputStream = new MemoryStream();    
        image.Save(outputStream, format);
        return outputStream.ToArray();
    }

    public static Result<string> OcrImage(string imageFilePath, string lang = "eng") =>    
        RunCmd(TesseractPath, $"{FailIfFileDoesNotExist(imageFilePath)} stdout -l {lang} --psm 1 --oem 1 --loglevel ERROR", Path.GetDirectoryName(TesseractPath)!);    
    
    public static Result<string> OcrImage(byte[] imageData, string lang = "eng") => 
        RunCmd(TesseractPath, $"stdin stdout -l {lang} --oem 1 --psm 1 --loglevel ERROR", imageData, Path.GetDirectoryName(TesseractPath)!);
    
    public static async Task<Result<string>> OcrImageAsync(byte[] imageData, string lang = "eng") => 
        await RunCmdAsync(TesseractPath, $"stdin stdout -l {lang} --oem 1 --psm 1 --loglevel ERROR", imageData, Path.GetDirectoryName(TesseractPath)!);
    
    public static async Task<Result<string>> ScanTextAsync(string lang = "eng")
    {
        var scanResult = await ScanAsync();
        if (scanResult.IsSuccess)
        {
            var imageBytes = scanResult.Value;
            if (imageBytes != null)
            {
                var sb = new StringBuilder();   
                foreach (var image in imageBytes)
                {
                    var result = await OcrImageAsync(image, lang);
                    if (result.IsSuccess)
                    {
                        sb.AppendLine(result.Value);
                    }
                    else
                    {
                        return Failure<string>($"Failed to convert image to text: {result.Message}", result.Exception);
                    }
                }
                return Success(sb.ToString());
            }
            else
            {
                return Failure<string>("No images scanned.");
            }
        }
        else
        {
            return Failure<string>(scanResult.Message, scanResult.Exception);
        }
    }

    public static async Task<Result<string>> OcrImageAsync(string filepath) => await OcrImageAsync(File.ReadAllBytes(filepath));

    
    public static async Task<string> GetDocumentText(string filePath)
    {
        try
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                Error("File does not exist: {FilePath}", filePath);
                return "";
            }

            string ext = file.Extension.ToLowerInvariant();
            if (ext == ".pdf")
            {
                var result = Documents.ConvertPdfToText(file.FullName);
                if (result.IsSuccess)
                {
                    return string.Join("\n", result.Value);
                }
                else
                {
                    //Error(result.Message, result.Exception);
                    return "";
                }
            }
            else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".tiff")
            {
                var result = await OcrImageAsync(file.FullName);
                if (result.IsSuccess)
                {
                    return result.Value;
                }
                else
                {
                    //Error(result.Exception, result.Message);
                    return "";
                }
            }
            else
            {
                Error("Unsupported file type: {FilePath}", filePath);
                return "";
            }
        }
        catch (Exception ex)
        {
            Error(ex, "Error getting document text for {FilePath}", filePath);
            return "";
        }
    }
    }


    

   
