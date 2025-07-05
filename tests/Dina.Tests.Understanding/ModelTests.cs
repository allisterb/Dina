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
          
            Model.Initialize(RuntimeType.OpenApiCompat, "http://localhost:8080/v1", "unsloth_gemma-3n-E2B-it-GGUF_gemma-3n-E2B-it-UD-Q4_K_XL.gguf");
        }

        [Fact]
        public void CanInitialize() => Assert.True(Model.IsInitialized);

        [Fact]
        public async Task CanStartChat()
        {
            var resp = Model.Prompt("hello my name is bob");
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