namespace Dina;


using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;

using OllamaSharp;
using LLama.Native;
using LLamaSharp.SemanticKernel;
using LLamaSharp.SemanticKernel.ChatCompletion;

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;




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
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        
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
        builder.AddOllamaChatCompletion(model, new Uri(llamaPath));
        //builder.Services.AddSingleton(chat);
        //builder.Plugins.AddFromType<TestFunctions>("Time");
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
        await foreach (var m in chat.GetStreamingChatMessageContentsAsync(messages, promptExecutionSettings, kernel))
        {
            if (m.InnerContent is not null && !string.IsNullOrEmpty(m.Content)) messages.Add((ChatMessageContent) m.InnerContent);    
            yield return m;
        }
    }

    public async Task CallFunction()
    {
        string prompt = "Finish the followin knock - knock joke.Knock, knock.Who's there ? { {$input} }, { {$input} } who ? ";
        KernelFunction jokeFunction =
            kernel.CreateFunctionFromPrompt(prompt);
        var c = kernel.CreateFunctionFromMethod(GetCurrentTime, "GetCurrentTime", "Get the current time for a city");
        var arguments = new KernelArguments()
        {
            ["input"] = "Boo"
        };
        //var joke = await kernel.InvokeAsync(jokeFunction,arguments);
        //Console.WriteLine(joke);

        //var j2 = kernel.ImportPluginFromType<TestFunctions>("Time");
        var plugin =
       KernelPluginFactory.CreateFromFunctions("Time",
                                       "Get the current time for a city",
                                       [KernelFunctionFactory.CreateFromMethod(GetCurrentTime)]);
        kernel.Plugins.Add(plugin);

        ChatCompletionAgent agent = new() // 👈🏼 Definition of the agent
        {
            Instructions = """
                   Answer questions about different locations.
                   For France, use the time format: HH:MM. HH goes from 00 to 23 hours, MM goes from 00 to 59 minutes.
                   """,
            Name = "Location Agent",
            Kernel = kernel,
            LoggerFactory = loggerFactory,
            // 👇🏼 Allows the model to decide whether to call the function
            Arguments = new KernelArguments(new OllamaPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([c], autoInvoke:true),
                
            }
                )
        };
        
        
        //agent.Kernel.Plugins.Add(plugin);
        /*
        //j2
        //var result = await kernel.InvokeAsync(j2["RandomTheme"]);

        /*
         var plugin =
     KernelPluginFactory.CreateFromFunctions("Time",
                                     "Get the current time for a city",
                                     [KernelFunctionFactory.CreateFromMethod(GetCurrentTime)]);
        


        */
        ChatHistory _chat =
[
    new ChatMessageContent(AuthorRole.User, "What time is it in Illzach, France?")
];
        
        /*
        //messages.AddUserMessage("What time is it in Illzach, France?");
        var response = await chat.GetChatMessageContentAsync("What time is it in Illzach, France?", new OllamaPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
        }, kernel);
        {
            Console.WriteLine(response);
            //Console.WriteLine(response.Content);
        }
        */

        await foreach (var m in agent.InvokeAsync(_chat, options:new AgentInvokeOptions() { KernelArguments = new KernelArguments(new OllamaPromptExecutionSettings
        {
            
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),

        })}))
       
        {
           Console.WriteLine(m.Message.ToString()); 
        }

    }

    public static void Test2()
    {
        var c = new OllamaApiClient("http://localhost:1344", OllamaModels.Gemma3n_2eb_tools);
        //c.ChatAsync(new OllamaSharp.Models.Chat.ChatRequest() { })
    }
    // 👇🏼 Define a time tool
    [KernelFunction, Description("Get the current time for a city")]
    [return: Description("The current time for a city")]
    string GetCurrentTime(string city) => $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";

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
    public const string Gemma3n_2eb_tools = "gemma3n:e2b_tools_test";
    public const string Gemma3_4b = "gemma3:4b";
    public const string Nomic_Embed_Text = "nomic-embed-text";
    #endregion

}
