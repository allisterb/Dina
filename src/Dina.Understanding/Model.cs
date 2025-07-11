﻿namespace Dina;

using System.ComponentModel;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using LLamaSharp.SemanticKernel.ChatCompletion;
using LLama.Native;
using OllamaSharp;
using LLamaSharp.SemanticKernel;

public enum ModelRuntime
{
    Ollama,
    LlamaCpp,
    OpenApiCompat
}  

public class ModelConversation : Runtime
{
    #region Constructors
    public ModelConversation(ModelRuntime runtimeType, string model, string llamaPath = "http://localhost:11434", string[]? systemPrompts = null)
    {
        this.runtimeType = runtimeType;
        if (runtimeType == ModelRuntime.Ollama)
        {
            var endpoint = new Uri(llamaPath);
#pragma warning disable SKEXP0001 
            var _client = new OllamaApiClient(endpoint, model);
            if (!_client.IsRunningAsync().Result)
            {
                throw new InvalidOperationException($"Ollama API at {llamaPath} is not running. Please start the Ollama server.");
            }
            client = _client;
#pragma warning disable SKEXP0070 
#pragma warning disable CS0618 
            chat = new OllamaChatCompletionService(model, endpoint, loggerFactory);
#pragma warning restore CS0618 
            promptExecutionSettings = new OllamaPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                NumPredict = 512,
                ExtensionData = new Dictionary<string, object>()
                {
                    
                }
            };
#pragma warning restore SKEXP0070 
#pragma warning restore SKEXP0001

            Info("Using Ollama API at {0} with model {1}", llamaPath, model);
        }
        else if (runtimeType == ModelRuntime.LlamaCpp)
        { 
            NativeLibraryConfig.LLama
                .WithAutoFallback(true)
                .WithCuda(false)
                .WithVulkan(false)
                .WithAvx(AvxLevel.None)
                .WithSearchDirectory(llamaPath)
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
                MaxTokens = 512,
                ExtensionData = new Dictionary<string, object>()
            };
            chat = new LLamaSharpChatCompletion(ex);
#pragma warning disable SKEXP0001 
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001 
            Info("Using llama.cpp embedded library at {0} with model {1}", llamaPath, model);
        }
        else
        {
#pragma warning disable SKEXP0010 
            chat = new OpenAIChatCompletionService(model, new Uri(llamaPath), loggerFactory: loggerFactory);
#pragma warning restore SKEXP0010 
#pragma warning disable SKEXP0001
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001 
            
            promptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                MaxTokens = 512,
                ExtensionData = new Dictionary<string, object>()
            };
            Info("Using OpenAI compatible API at {0} with model {1}", llamaPath, model);
        }
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chat);
        kernel = builder.Build();
        if (systemPrompts != null)
        {
            foreach (var systemPrompt in systemPrompts)
            {
                messages.AddSystemMessage(systemPrompt);
            }
        }   
    }
    #endregion

    #region Methods
    public IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, params object[] args)
    {
        var messageItems = new ChatMessageContentItemCollection()
        {
            new Microsoft.SemanticKernel.TextContent(string.Format(prompt, args))   
        };
        messages.AddUserMessage(messageItems);
        return chat.GetStreamingChatMessageContentsAsync(messages, promptExecutionSettings, kernel);
    }

    public IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, byte[]? imageData = null, string imageMimeType = "image/jpeg") 
    {
        if (imageData != null)
        {
            messages.AddUserMessage(new ChatMessageContentItemCollection {
                new Microsoft.SemanticKernel.TextContent(prompt),
                new ImageContent(imageData, imageMimeType),
                
            });
        }
        else
        {
            messages.AddUserMessage(prompt);
        }
        return chat.GetStreamingChatMessageContentsAsync(messages, promptExecutionSettings, kernel);        
    }
    #endregion

    #region Fields

    public ModelRuntime runtimeType;

    public Kernel kernel = new Kernel();

    public IChatClient client;
    
    public IChatCompletionService chat;

    public ChatHistory messages = new ChatHistory();

    public PromptExecutionSettings promptExecutionSettings;
    #endregion
}

public class OllamaModels
{
    #region Constants
    public const string Gemma3_4b_it_q4_K_M = "gemma3:4b-it-q4_K_M";
    public const string Gemma3n_2eb = "gemma3n:e2b";
    public const string Gemma3_4b = "gemma3:4b";
    public const string Nomic_Embed_Text = "nomic-embed-text";
    #endregion

}
