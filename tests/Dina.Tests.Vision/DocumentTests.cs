namespace Dina.Tests.Vision;

using Serilog;
using Serilog.Extensions.Logging;

public class DocumentTests
{
    static DocumentTests()
    {
        var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.log"))
                .CreateLogger();
        var lf = new SerilogLoggerFactory(logger);
        var lp = new SerilogLoggerProvider(logger, false);
        Runtime.Initialize("Dina.Core.Understanding", "Tests", false, lf, lp);
        Documents.MuPdfPath = "C:\\Projects\\Dina\\bin";
    }

    [Fact]
    public void CanConvertPdfToText()
    {
        var r = Documents.ConvertPdfToText("..\\..\\..\\..\\data\\test3.pdf");
        Assert.True(r.IsSuccess);
        Assert.NotEmpty(r.Value);
        Assert.Contains("6.7. Trends and business cycles", r.Value[0]); 
    }

    [Fact]
    public void CanConvertPdf()
    {
        var r = Documents.ConvertPdfToImages("..\\..\\..\\..\\data\\test.pdf");
        Assert.True(r.IsSuccess);
        Assert.NotEmpty(r.Value);
    }

    [Fact]
    public async Task CanGetScannerNames()
    { 
        var scanners = await Documents.GetScannerNames();
        Assert.NotEmpty(scanners);  
    }

    [Fact]
    public async Task CanScan()
    {
        var images = await Documents.Scan();
        Assert.NotEmpty(images);
    }

    [Fact]
    public void CanOpenCvScan()
    {
        //Documents.ScanDocument("..\\..\\..\\..\\data\\eevaccinecard.jpg", saveImage: true);
        //Assert.NotEmpty(images);

        var s = new OpenCvDocumentScanner();
        s.Scan("..\\..\\..\\..\\data\\eevaccinecard.jpg", ".");
    }
}