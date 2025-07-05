namespace Dina;
  
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using LLama;
using LLama.Common;
using LLama.Native;
using LLamaSharp.SemanticKernel.ChatCompletion;

public enum ModelRuntimeType
{
    LlamaCpp,
    OpenApiCompat
}  

public class Model : Runtime
{
    #region Constructors
    public static bool Initialize(ModelRuntimeType runtimeType, string llamacppPath, string modelPath)
    {
        if (runtimeType == ModelRuntimeType.LlamaCpp)
        { 
            NativeLibraryConfig.LLama
                .WithAutoFallback(true)
                .WithCuda(false)
                .WithVulkan(false)
                .WithAvx(AvxLevel.None)
                .WithSearchDirectory(llamacppPath)
                .WithLogCallback(Runtime.logger);


            var parameters = new ModelParams(modelPath)
            {
                GpuLayerCount = 99 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
            };
            
            LLamaWeights model = LLamaWeights.LoadFromFile(parameters);
            var ex = new StatelessExecutor(model, parameters, Runtime.logger);
            chat = new LLamaSharpChatCompletion(ex);
            Info("Using llama.cpp embedded library at {0} with model {1}", llamacppPath, modelPath);
        }
        else
        {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            chat = new OpenAIChatCompletionService(modelPath, new Uri(llamacppPath));
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            
            Info("Using OpenAI compatible API at {0} with model {1}", llamacppPath, modelPath);
        }
        
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(Runtime.logger);
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        kernel = builder.Build();
        isInitialized = true;
        return isInitialized;
    }
    #endregion

    #region Methods
    private static void ThrowIfNotInitialized()
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException("ModelRuntime is not initialized. Call Initialize method first.");
        }
    }   

    public static IAsyncEnumerable<StreamingChatMessageContent> Prompt(string prompt, byte[]? imageData = null, string imageMimeType = "image/jpeg") 
    {
        ThrowIfNotInitialized();
        if (imageData != null)
        {
            chatHistory.AddUserMessage([
                new TextContent(prompt),
                new ImageContent(imageData, imageMimeType),
            ]);
        }
        else
        {
            chatHistory.AddUserMessage(prompt);
        }
        return chat.GetStreamingChatMessageContentsAsync(chatHistory, kernel: kernel);        
    }
    #endregion

    #region Fields
    public static bool isInitialized = false;
    
    public static Kernel kernel = new Kernel();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public static IChatCompletionService chat = new OpenAIChatCompletionService("unsloth_gemma-3n-E2B-it-GGUF_gemma-3n-E2B-it-UD-Q4_K_XL.gguf", new Uri("http://localhost:8080/v1"), null);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public static Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
    #endregion
}
