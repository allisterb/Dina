namespace Dina.Tests.Understanding;

using Serilog;
using Serilog.Extensions.Logging;


public class MemoryTests
{
    static MemoryTests()
    {
        var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.log"))
                .CreateLogger();
        var lf = new SerilogLoggerFactory(logger);
        var lp = new SerilogLoggerProvider(logger, false);
        Runtime.Initialize("Dina.Understanding", "Tests", false, lf, lp);
        Documents.BinPath = "C:\\Projects\\Dina\\bin";
    }
    /*

    [Fact]
    public async Task CanSearchMemory()
    {
        var m = new Memory(ModelRuntime.Ollama, OllamaModels.Gemma3n_2eb, OllamaModels.Nomic_Embed_Text);
        var r = await m.("..\\..\\..\\..\\data\\test.pdf", "test");
        Assert.True(r.IsSuccess);
        var rx = await m.SearchAsync("data analysis", "test");
        string s = "";
        //Assert.NotNull(rx);
        foreach (var response in rx.Value.Results)
        {
            Assert.NotNull(response.SourceName);
            s += response;
        }
        Assert.NotNull(s);
        Console.WriteLine(s);
    }

    [Fact]
    public async Task CanAskMemory()
    {
        var m = new Memory(ModelRuntime.Ollama, OllamaModels.Gemma3n_2eb, OllamaModels.Nomic_Embed_Text);
        var r = await m.ImportAsync("..\\..\\..\\..\\data\\test.pdf", "test");
        Assert.True(r.IsSuccess);
        var rx = m.AskAsync("What does the document say ?", "test");
        string s = "";
        Assert.NotNull(rx);
        await foreach (var response in rx)
        {
            Assert.NotNull(response);
            s += response;
        }
        Assert.NotNull(s);
        Console.WriteLine(s);
    }
    */

}