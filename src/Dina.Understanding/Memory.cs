namespace Dina;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;

using static Result;

public class Memory : Runtime
{
    public Memory(ModelRuntime modelRuntime, string textmodel, string embeddingmodel, string llamapath = "http://localhost:11434")
    {
        this.modelRuntime = modelRuntime;
        var builder = new KernelMemoryBuilder();
        var ollamaconfig = new OllamaConfig()
        {
            Endpoint = llamapath,
            TextModel = new OllamaModelConfig() { 
                ModelName = textmodel,
                MaxTokenTotal = 32 * 1024,
                NumGpu = 35 
            },
            EmbeddingModel = new OllamaModelConfig() { 
                ModelName = embeddingmodel,
                MaxTokenTotal = 2048,
                NumGpu = 35
            }
        };  
        builder.Services.AddLogging(configure => configure.AddProvider(loggerProvider));
        memory =
            builder
            .WithOllamaTextGeneration(ollamaconfig, new CL100KTokenizer())   
            .WithOllamaTextEmbeddingGeneration(ollamaconfig, new CL100KTokenizer())
            .Build<MemoryServerless>();
    }

    public async Task<Result<string>> ImportAsync(string path, string index) => await ExecuteAsync(memory.ImportDocumentAsync(path, index:index));

    public IAsyncEnumerable<MemoryAnswer> AskAsync(string question, string index) => memory.AskStreamingAsync(question, index:index);

    #region Fields
    ModelRuntime modelRuntime;
    IKernelMemory memory;
    #endregion
}
