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
            Runtime.Initialize("Dina.Core.Understanding", "Tests", false, lf, lp);
            //
            //Model.Initialize(ModelRuntimeType.OpenApiCompat, "http://localhost:8080/v1", "C:\\Users\\Allister\\AppData\\Local\\llama.cpp\\unsloth_gemma-3n-E2B-it-GGUF_gemma-3n-E2B-it-UD-Q4_K_XL.gguf");
            //Model.Initialize(ModelRuntimeType.OpenApiCompat, "http://localhost:8080/v1", "D:\\Models\\gemma-3-4b-it-Q4_K_M.gguf");

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

    }
}