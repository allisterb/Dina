﻿namespace Dina;

using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OllamaSharp;
using LLama.Native;
using LLamaSharp.SemanticKernel;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using LLamaSharp.SemanticKernel.TextEmbedding;

public enum ModelRuntime
{
    Ollama,
    LlamaCpp,
    OpenApiCompat
}

public static class OllamaModels
{
    #region Constants
    public const string Gemma3_4b_it_q4_K_M = "gemma3:4b-it-q4_K_M";
    public const string Gemma3n_2eb = "gemma3n:e2b";
    public const string Gemma3n_2eb_tools = "gemma3n:e2b_tools_test";
    public const string Gemma3_4b = "gemma3:4b";
    public const string Nomic_Embed_Text = "nomic-embed-text";
    public const string All_MiniLm = "all-minilm";
    #endregion

}

public class ModelConversation : Runtime
{
    #region Constructors
    public ModelConversation(ModelRuntime runtimeType = ModelRuntime.Ollama, string model = OllamaModels.Gemma3n_2eb_tools, string runtimePath = "http://localhost:11434", string[]? systemPrompts = null)
    {
        this.runtimeType = runtimeType;
        this.runtimePath = runtimePath;
        this.model = model;
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(builder =>
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(loggerProvider)
            );
        if (runtimeType == ModelRuntime.Ollama)
        {
            var endpoint = new Uri(runtimePath);
#pragma warning disable SKEXP0001, SKEXP0070 
            var _client = new OllamaApiClient(endpoint, model);
            
            if (!_client.IsRunningAsync().Result)
            {
                throw new InvalidOperationException($"Ollama API at {runtimePath} is not running. Please start the Ollama server.");
            }
            client = _client;
            chat = _client.AsChatCompletionService(kernel.Services);
            builder.AddOllamaEmbeddingGenerator(new OllamaApiClient(endpoint, OllamaModels.All_MiniLm));
            //embeddingService = _client.AsEmbeddingGenerationService(kernel.Services);   
#pragma warning restore SKEXP0070, SKEXP0001 
            promptExecutionSettings = new OllamaPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
                ExtensionData = new Dictionary<string, object>()
            };

            Info("Using Ollama API at {0} with model {1}", runtimePath, model);
        }
        else if (runtimeType == ModelRuntime.LlamaCpp)
        {
            NativeLibraryConfig.LLama
                .WithAutoFallback(true)
                .WithCuda(false)
                .WithVulkan(false)
                .WithAvx(AvxLevel.None)
                .WithSearchDirectory(runtimePath)
                .WithLogCallback(logger);

            var parameters = new LLama.Common.ModelParams(model)
            {
                FlashAttention = true,
                GpuLayerCount = 35 // How many layers to offload to GPU.
            };

            LLama.LLamaWeights lm = LLama.LLamaWeights.LoadFromFile(parameters);
            var ex = new LLama.StatelessExecutor(lm, parameters, logger);
            promptExecutionSettings = new LLamaSharpPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
                ExtensionData = new Dictionary<string, object>()
            };
            //embeddingService = new LLamaSharpEmbeddingGenerationService(ex, parameters, loggerFactory);
            chat = new LLamaSharpChatCompletion(ex);
#pragma warning disable SKEXP0001,SKEXP0010 
            client = chat.AsChatClient();
            Info("Using llama.cpp embedded library at {0} with model {1}", runtimePath, model);
        }
        else
        {
            chat = new OpenAIChatCompletionService(model, new Uri(runtimePath), loggerFactory: loggerFactory); 
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001, SKEXP0010
            promptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true), 
                ExtensionData = new Dictionary<string, object>()
            };
            Info("Using OpenAI compatible API at {0} with model {1}", runtimePath, model);
        }
        
        builder.Services
            .AddChatClient(client)
            .UseFunctionInvocation(loggerFactory);
        kernel = builder.Build();
        
        vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions() { EmbeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>() });
        
        if (systemPrompts != null)
        {
            foreach (var systemPrompt in systemPrompts)
            {
                messages.AddSystemMessage(systemPrompt);
            }
        }  
    }
    #endregion

    #region Methods and Properties

    public ModelConversation AddPlugin<T>(string pluginName)
    {
        kernel.Plugins.AddFromType<T>(pluginName);
        return this;
    }

    public ModelConversation AddPlugin<T>(T obj, string pluginName)
    {
#pragma warning disable SKEXP0120 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        kernel.Plugins.AddFromObject<T>(obj, jsonSerializerOptions: new System.Text.Json.JsonSerializerOptions(), pluginName: pluginName);
#pragma warning restore SKEXP0120 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return this;
    }
    public async IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, params object[] args)
    {
        var messageItems = new ChatMessageContentItemCollection()
        {
            new Microsoft.SemanticKernel.TextContent(string.Format(prompt, args))
        };
        messages.AddUserMessage(messageItems);
        StringBuilder sb = new StringBuilder();
        await foreach (var m in chat.GetStreamingChatMessageContentsAsync(messages, promptExecutionSettings, kernel))
        {
            if (m.Content is not null && !string.IsNullOrEmpty(m.Content))
            {
                sb.Append(m.Content);
                yield return m;
            }
        }
        messages.AddAssistantMessage(sb.ToString());
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, byte[]? imageData, string imageMimeType = "image/jpeg")
    {
        messages.AddUserMessage(new ChatMessageContentItemCollection {
            new Microsoft.SemanticKernel.TextContent(prompt),
            new ImageContent(imageData, imageMimeType),

        });
        StringBuilder sb = new StringBuilder();
        await foreach (var m in chat.GetStreamingChatMessageContentsAsync(messages, promptExecutionSettings, kernel))
        {
            if (m.Content is not null && !string.IsNullOrEmpty(m.Content))
            {
                sb.Append(m.Content);
                yield return m;
            }
        }
        messages.AddAssistantMessage(sb.ToString());
    }
    #endregion
    public VectorStore vectorStore;

    #region Fields

    public ModelRuntime runtimeType;

    public string runtimePath;

    public string model;    

    public Kernel kernel = new Kernel();

    public IChatClient client;

    public IChatCompletionService chat;

    public ChatHistory messages = new ChatHistory();

    public PromptExecutionSettings promptExecutionSettings;
    #endregion
}
