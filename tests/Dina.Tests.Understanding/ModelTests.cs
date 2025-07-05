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
                 .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.log"))
                 
                 .CreateLogger();
            Runtime.Initialize("Dina", "Tests", false, new SerilogLoggerFactory(logger).CreateLogger("Dina"));
            //
            //Model.Initialize(ModelRuntimeType.OpenApiCompat, "http://localhost:8080/v1", "C:\\Users\\Allister\\AppData\\Local\\llama.cpp\\unsloth_gemma-3n-E2B-it-GGUF_gemma-3n-E2B-it-UD-Q4_K_XL.gguf");
            Model.Initialize(ModelRuntimeType.OpenApiCompat, "http://localhost:8080/v1", "D:\\Models\\gemma-3-4b-it-Q4_K_M.gguf");

        }

        [Fact]
        public void CanInitialize() => Assert.True(Model.isInitialized);

        [Fact]
        public async Task CanStartChat()
        {
            var resp = Model.Prompt("What kind of document is this?", File.ReadAllBytes("C:\\OneDrive\\Pictures\\Other\\Eunice\\travel_authorization.png"), "image/png");
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