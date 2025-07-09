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
            var mc = new ModelConversation(ModelRuntime.Ollama, OllamaModels.Gemma3n_2eb);
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
            var mc = new ModelConversation(ModelRuntime.Ollama, OllamaModels.Gemma3n_2eb);
            var pdfbin = File.ReadAllBytes("..\\..\\..\\..\\data\\test.pdf");   
            var pdftext = Documents.ConvertPdfToText("..\\..\\..\\..\\data\\test.pdf");
            Assert.True(pdftext.IsSuccess);
            var resp = mc.Prompt("What is the first question on the document: Document:{0}", pdftext.Value[0]) ;
            //var resp = mc.Prompt("What is the first question on the text: \n Text:{0}", pdftext.Value[0]);
            string s = "";
            Assert.NotNull(resp);
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);
 
        }
    }
}