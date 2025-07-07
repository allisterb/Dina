using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Extensions.Logging;
namespace Dina.Tests.Understanding
{
    public class ModelTests
    {
        static ModelTests()
        {
            var logger = new LoggerConfiguration()
                 .Enrich.FromLogContext()
                 .WriteTo.Console() 
                 .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.log"))
                 .CreateLogger();
            var lf = new SerilogLoggerFactory(logger);
            var lp = new SerilogLoggerProvider(logger, false);
            Runtime.Initialize("Dina.Understanding", "Tests", false, lf, lp);
            Documents.MuPdfPath = "C:\\Projects\\Dina\\bin";
        }

        [Fact]
        public async Task CanStartOllamaGemini3nChat()
        {
            var mc = new ModelConversation(ModelRuntime.Ollama, OllamaModels.Gemma3_4b);
            var resp = mc.Prompt("What kind of image is this?", File.ReadAllBytes("C:\\Users\\Allister\\Pictures\\applogo.png"), "image/png");
            string s = "";
            Assert.NotNull(resp);
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);
            Console.WriteLine(s);
        }

        [Fact]
        public async Task CanAskGemini3nAboutPdf()
        {
            var mc = new ModelConversation(ModelRuntime.Ollama, OllamaModels.Gemma3_4b);
            var pdf = Documents.ConvertPdfToImages("..\\..\\..\\..\\data\\test.pdf");
            Assert.True(pdf.IsSuccess);
            var resp = mc.Prompt("What kind of image is this?", pdf.Value[0], "image/png");
            string s = "";
            Assert.NotNull(resp);
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);
            Console.WriteLine(s);
        }
    }
}