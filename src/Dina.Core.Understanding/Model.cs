namespace Dina;


using System.ComponentModel;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using LLama.Native;
using LLamaSharp.SemanticKernel.ChatCompletion;

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
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var _client = new OllamaApiClient(endpoint, model);
            if (!_client.IsRunningAsync().Result)
            {
                throw new InvalidOperationException($"Ollama API at {llamaPath} is not running. Please start the Ollama server.");
            }
            client = _client;
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable CS0618 // Type or member is obsolete
            chat = new OllamaChatCompletionService(model, endpoint, loggerFactory);
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            promptExecutionSettings = new OllamaPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                NumPredict = 512,
                ExtensionData = new Dictionary<string, object>()
                {
                    { "num_gpu", 30 } // Ollama specific setting for number of layers to offload to GPU.
                }
            };
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
                MaxTokens = 512,
                ExtensionData = new Dictionary<string, object>()
            };
            Info("Using OpenAI compatible API at {0} with model {1}", llamaPath, model);
        }

        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(l => l.AddProvider(loggerProvider));
        builder.Services.AddSingleton<IChatCompletionService>(chat);
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
    public IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, byte[]? imageData = null, string imageMimeType = "image/jpeg") 
    {
        var describeLambda = [Description("Describe image")] (
    [Description("Short description")] string shortDescription,
    [Description("Long description")] string longDescription
) =>
        {
            return;
        };

        //var function = KernelFunctionFactory.CreateFromMethod(describeLambda, "describe");
        //this.promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Required([function], autoInvoke: false);


        if (imageData != null)
        {
            //messages.AddSystemMessage("you are an expert technician that will extract information from images");
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
    public const string Gemma3n_2eb = "gemma3n:2eb";
    public const string Gemma3_4b = "gemma3:4b";
    #endregion

}
