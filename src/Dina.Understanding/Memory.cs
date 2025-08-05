namespace Dina;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Ollama;

using static Result;

public class Memory : Runtime
{
    public Memory(ModelRuntime modelRuntime, string textmodel, string embeddingmodel, string endpoint = "http://localhost:11434")
    {
        this.modelRuntime = modelRuntime;
        this.ollamaconfig = new OllamaConfig()
        {
            Endpoint = endpoint,
            TextModel = new OllamaModelConfig(textmodel, 32 * 1024),
            EmbeddingModel = new OllamaModelConfig(embeddingmodel, 2048)
        };

        var builder = new KernelMemoryBuilder();
        builder.Services.AddLogging(configure => 
            configure
            .AddProvider(loggerProvider)
            .SetMinimumLevel(LogLevel.Trace));
        
        this.memory =
            builder
            .WithOllamaTextGeneration(ollamaconfig, new CL100KTokenizer())   
            .WithOllamaTextEmbeddingGeneration(ollamaconfig, new CL100KTokenizer())            
            .Build<MemoryServerless>();
        this.plugin = new MemoryPlugin(memory, waitForIngestionToComplete: true, defaultIndex: "KB");
    }

    public async Task<Result<string>> ImportTextAsync(string text, string id, string index)
        => await ExecuteAsync(memory.ImportTextAsync(text, index: index, documentId: id), "Imported document id {0} to index {1}.", val: r => r, args:index);

    public async Task<Result<string>> ImportAsync(string path, string index) 
        => await ExecuteAsync(memory.ImportDocumentAsync(path, index: index, documentId:path), "Imported document {0} to index {1} with id {2}.",
            "Failed to import document {0} to index {1}.", r => r, path, index);
    
    public IAsyncEnumerable<MemoryAnswer> AskAsync(string question, string index) => memory.AskStreamingAsync(question, index:index);

    public async Task<Result<SearchResult>> SearchAsync(string query, string index) 
        => await ExecuteAsync(memory.SearchAsync(query, index: index), "Query \"{0}\" of index {1} returned {2} results", "", (r) => r.Results.Count.ToString(), query, index);

    #region Fields
    public readonly MemoryPlugin plugin;
    readonly ModelRuntime modelRuntime;
    IKernelMemory memory;
    readonly OllamaConfig ollamaconfig;  
    
    #endregion
}
