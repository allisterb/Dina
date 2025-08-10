namespace Dina;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.SemanticKernelPlugin.Internals;

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
        this.plugin = new MemoryPlugin(this, waitForIngestionToComplete: false, defaultIndex: "kb");
    }

    public async Task<Result<int>> CreateKBAsync(string path)
    {
        kbindex.Clear();
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            var text = await Documents.GetDocumentText(file);
            if (!string.IsNullOrEmpty(text))
            {
                var id = file.GetHashCode();
                kbindex.Add(id, file);
                await memory.ImportTextAsync(text, id.ToString(), index: "kb").ConfigureAwait(false);
            }
        }
        if (kbindex.Count > 0)
        {
            return Success(kbindex.Count);
        }
        else
        {
            return Failure<int>("Did not import any files from knowledge base.");
        }
    }

    internal async Task<SearchResult> SearchAsync(        
        string query,
        string? index = null,
        double minRelevance = 0,
        int limit = 1,
        TagCollectionWrapper? tags = null,
        CancellationToken cancellationToken = default)
    {
        return await this.memory
            .SearchAsync(
                query: query,
                index: index ?? "kb" ,
                filter: TagsToMemoryFilter(tags),
                minRelevance: minRelevance,
                limit: limit,
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static MemoryFilter? TagsToMemoryFilter(TagCollection? tags)
    {
        if (tags == null)
        {
            return null;
        }

        var filters = new MemoryFilter();

        foreach (var tag in tags)
        {
            filters.Add(tag.Key, tag.Value);
        }

        return filters;
    }
    /*
    public async Task<Result<string>> ImportTextAsync(string text, string id, string index)
        => await ExecuteAsync(memory.ImportTextAsync(text, index: index, documentId: id), "Imported document id {0} to index {1}.", val: r => r, args:index);

    public async Task<Result<string>> ImportAsync(string path, string index) 
        => await ExecuteAsync(memory.ImportDocumentAsync(path, index: index, documentId:path), "Imported document {0} to index {1} with id {2}.",
            "Failed to import document {0} to index {1}.", r => r, path, index);
    
    public IAsyncEnumerable<MemoryAnswer> AskAsync(string question, string index) => memory.AskStreamingAsync(question, index:index);

    public async Task<Result<SearchResult>> SearchAsync(string query, string index) 
        => await ExecuteAsync(memory.SearchAsync(query, index: index), "Query \"{0}\" of index {1} returned {2} results", "", (r) => r.Results.Count.ToString(), query, index);
    */



    #region Fields
    public readonly MemoryPlugin plugin;
    Dictionary<int, string> kbindex = new Dictionary<int, string>();
    readonly ModelRuntime modelRuntime;
    internal IKernelMemory memory;
    readonly OllamaConfig ollamaconfig;  
    #endregion
}
