namespace Dina;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;

using LLama.Native;
using LLamaSharp.SemanticKernel.ChatCompletion;
using OllamaSharp;

public enum ModelRuntimeType
{
    Ollama,
    LlamaCpp,
    OpenApiCompat
}  

public class ModelConversation : Runtime
{
    #region Constructors
    public ModelConversation(ModelRuntimeType runtimeType, string llamaPath, string model)
    {
        this.runtimeType = runtimeType;
        if (runtimeType == ModelRuntimeType.Ollama)
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var _client = new OllamaApiClient(new OllamaApiClient.Configuration() { Model = model, Uri = new Uri(llamaPath) });
            if (!_client.IsRunningAsync().Result)
            {
                throw new InvalidOperationException($"Ollama API at {llamaPath} is not running. Please start the Ollama server.");
            }
            client = _client;   
            chat = client.AsChatCompletionService();
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            promptExecutionSettings = new OllamaPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                NumPredict = 1024,
                ExtensionData = new Dictionary<string, object>()
                {
                    { "num_gpu", 30 } // Ollama specific setting for number of layers to offload to GPU.
                }
            };
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            Info("Using Ollama API at {0} with model {1}", llamaPath, model);
        }
        else if (runtimeType == ModelRuntimeType.LlamaCpp)
        { 
            NativeLibraryConfig.LLama
                .WithAutoFallback(true)
                .WithCuda(false)
                .WithVulkan(false)
                .WithAvx(AvxLevel.None)
                .WithSearchDirectory(llamaPath)
                .WithLogCallback(Runtime.logger);

            var parameters = new LLama.Common.ModelParams(model)
            {
                GpuLayerCount = 30 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
            };
            
            LLama.LLamaWeights lm = LLama.LLamaWeights.LoadFromFile(parameters);
            var ex = new LLama.StatelessExecutor(lm, parameters, Runtime.logger);
            chat = new LLamaSharpChatCompletion(ex);
            promptExecutionSettings = new PromptExecutionSettings()
            {
                ExtensionData = new Dictionary<string, object>()
            };
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            
            Info("Using llama.cpp embedded library at {0} with model {1}", llamaPath, model);
        }
        else
        {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            chat = new OpenAIChatCompletionService(model, new Uri(llamaPath));
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            
            promptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                MaxTokens = 1024,
                ExtensionData = new Dictionary<string, object>()
            };
            Info("Using OpenAI compatible API at {0} with model {1}", llamaPath, model);
        }
        
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(Runtime.logger);
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        kernel = builder.Build();
    }
    #endregion

    #region Methods
    public IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, byte[]? imageData = null, string imageMimeType = "image/jpeg") 
    {
        if (imageData != null)
        {
            chatHistory.AddUserMessage([
                new Microsoft.SemanticKernel.TextContent(prompt),
                new ImageContent(imageData, imageMimeType),
            ]);
        }
        else
        {
            chatHistory.AddUserMessage(prompt);
        }
        return chat.GetStreamingChatMessageContentsAsync(chatHistory, kernel: kernel, executionSettings: promptExecutionSettings);        
    }
    #endregion

    #region Fields

    public ModelRuntimeType runtimeType;

    public Kernel kernel = new Kernel();

    public IChatClient client;
    
    public IChatCompletionService chat;

    public ChatHistory chatHistory = new ChatHistory();

    public PromptExecutionSettings promptExecutionSettings;
    #endregion
}
