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
    public Memory(ModelRuntime modelRuntime, string textmodel, string embeddingmodel, string llamapath = "http://localhost:11434")
    {
        this.modelRuntime = modelRuntime;
        var builder = new KernelMemoryBuilder();
        ollamaconfig = new OllamaConfig()
        {
            Endpoint = llamapath,
            TextModel = new OllamaModelConfig() { 
                ModelName = textmodel,
            },
            EmbeddingModel = new OllamaModelConfig() { 
                ModelName = embeddingmodel,
            }
        };  
     
        builder.Services.AddLogging(configure => 
            configure
            .AddProvider(loggerProvider)
            .SetMinimumLevel(LogLevel.Debug));
        
        memory =
            builder
            .WithOllamaTextGeneration(ollamaconfig, new CL100KTokenizer())   
            .WithOllamaTextEmbeddingGeneration(ollamaconfig, new CL100KTokenizer())
            
            .Build<MemoryServerless>();
    }

    public async Task<Result<string>> ImportAsync(string path, string index) 
        => await ExecuteAsync(memory.ImportDocumentAsync(path, index: index), "Imported document {0} to index {1} with id {2}.",
            "Failed to import document {0} to index {1}.", r => r, path, index);
    
    public IAsyncEnumerable<MemoryAnswer> AskAsync(string question, string index) => memory.AskStreamingAsync(question, index:index);

    public async Task<Result<SearchResult>> SearchAsync(string query, string index) 
        => await ExecuteAsync(memory.SearchAsync(query, index: index), "Query \"{0}\" of index {1} returned {2} results", "", (r) => r.Results.Count.ToString(), query, index);

    #region Fields
    ModelRuntime modelRuntime;
    IKernelMemory memory;
    OllamaConfig ollamaconfig;  
    #endregion
}
