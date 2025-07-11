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

    public static void ScanDocument(Size windowSize, string? filePath = null, bool saveImage = false)
    {
        Mat perspectiveTransform(Mat image, Point2f[] pts)
        {
            // Order points: top-left, top-right, bottom-right, bottom-left.
            Point2f[] rect = orderPoints(pts);

            // Calculate width of the new image.
            double widthA = distance(rect[2], rect[3]);
            double widthB = distance(rect[1], rect[0]);
            double maxWidth = Math.Max(widthA, widthB);

            // Calculate height of the new image.
            double heightA = distance(rect[1], rect[2]);
            double heightB = distance(rect[0], rect[3]);
            double maxHeight = Math.Max(heightA, heightB);

            Point2f[] dst = new Point2f[]
            {
                    new Point2f(0, 0),
                    new Point2f((float)maxWidth - 1, 0),
                    new Point2f((float)maxWidth - 1, (float)maxHeight - 1),
                    new Point2f(0, (float)maxHeight - 1)
            };

            Mat M = Cv2.GetPerspectiveTransform(rect, dst);
            Mat warped = new Mat();
            Cv2.WarpPerspective(image, warped, M, new Size((int)maxWidth, (int)maxHeight));
            return warped;
        }

        double distance(Point2f p1, Point2f p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    
        void analyze(Mat frame, string? saveImagePath = null)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

            Mat edges = new Mat();
            Cv2.Canny(blurred, edges, 50, 150);

            // Find contours
            Cv2.FindContours(edges, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // Sort contours by area descending and examine up to 5
            var candidateContours = contours.OrderByDescending(c => Cv2.ContourArea(c)).Take(5);

            Point2f[]? scanDocument = null;
            foreach (var contour in candidateContours)
            {
                double perimeter = Cv2.ArcLength(contour, true);
                Point[] approx = Cv2.ApproxPolyDP(contour, 0.02 * perimeter, true);
                if (approx.Length == 4)
                {
                    scanDocument = Array.ConvertAll(approx, p => new Point2f(p.X, p.Y));
                    break;
                }
            }

            if (scanDocument is not null)
            {
                // Perform perspective transform
                Mat warped = perspectiveTransform(frame, scanDocument);
                Mat resizedWarped = new Mat();
                Cv2.Resize(warped, resizedWarped, windowSize);

                Mat resizedOriginal = new Mat();
                Cv2.Resize(frame, resizedOriginal, windowSize);

                Cv2.ImShow("DataFlair", resizedWarped);
                Cv2.ImShow("Original Document", resizedOriginal);

                if (saveImagePath is not null)
                {
                    Cv2.ImWrite(saveImagePath, warped);  
                }
            }
        }

        Point2f[] orderPoints(Point2f[] pts)
        {
            Point2f[] rect = new Point2f[4];

            // The top-left point will have the smallest sum,
            // the bottom-right will have the largest sum.
            var sum = pts.Select(p => p.X + p.Y).ToArray();
            rect[0] = pts[Array.IndexOf(sum, sum.Min())];
            rect[2] = pts[Array.IndexOf(sum, sum.Max())];

            // The top-right will have the smallest difference,
            // the bottom-left will have the largest difference.
            var diff = pts.Select(p => p.X - p.Y).ToArray();
            rect[1] = pts[Array.IndexOf(diff, diff.Min())];
            rect[3] = pts[Array.IndexOf(diff, diff.Max())];

            return rect;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            var cap = new VideoCapture(1);
            if (!cap.IsOpened())
            {
                Error("Error: Unable to open capture source");
                return;
            }
            while (true)
            {
                Mat frame = new Mat();
                if (!cap.Read(frame) || frame.Empty())
                    break;
                analyze(frame);

            }

            cap.Release();
            Cv2.DestroyAllWindows();
        }
        else
        {
            Mat frame = Cv2.ImRead(ViewFilePath(filePath), ImreadModes.Color);
            analyze(frame);
        }
    }
}
