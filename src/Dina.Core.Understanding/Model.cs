namespace Dina;

using LLama;
using LLama.Common;
using LLama.Native;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;  
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp;
using System.Linq;
using System.Runtime.InteropServices;


public enum RuntimeType
{
    LlamaCpp,
    OpenApiCompat
}  

public class Model : Runtime
{
    public static bool Initialize(RuntimeType runtimeType, string llamacppPath, string modelPath)
    {
        if (runtimeType == RuntimeType.LlamaCpp)
        {
            Info("Using llama.cpp embedded library at {0} with model {1}", llamacppPath, modelPath);    
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

            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IChatCompletionService>((serviceProvider) => chat);

            kernel = builder.Build();
            IsInitialized = true;
        }
        else
        {
            Info("Using OpenAI compatible API at {0} with model {1}", llamacppPath, modelPath);
            var builder = Kernel.CreateBuilder();
            kernel = 
                builder.AddOpenAIChatCompletion(modelPath, new Uri(llamacppPath), null)
                .Build();
            IsInitialized = true;
        }
        
        return IsInitialized;
    }

    public static bool IsInitialized = false;

    public static async IAsyncEnumerable<string> Prompt(string prompt) 
    {
        await foreach (var r in kernel.InvokePromptStreamingAsync(prompt))
        {
            yield return r.ToString();
        }
    }

    [DllImport("ggml", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ggml_backend_load_all();

    public static LLamaSharpChatCompletion? chat;

    public static Kernel kernel = new Kernel(); 

}
