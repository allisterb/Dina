namespace Dina;

using DocumentFormat.OpenXml.Wordprocessing;
using LLama.Native;
using LLamaSharp.SemanticKernel;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

public enum ModelRuntime
{
    Ollama,
    LlamaCpp,
    OpenApiCompat
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

public class ModelConversation : Runtime
{
    #region Constructors
    public ModelConversation(ModelRuntime runtimeType, string model, string runtimePath = "http://localhost:11434", string[]? systemPrompts = null)
    {
        this.runtimeType = runtimeType;
        this.runtimePath = runtimePath;
        this.model = model;
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
            promptExecutionSettings = new OllamaPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
                ExtensionData = new Dictionary<string, object>()
            };
#pragma warning restore SKEXP0070, SKEXP0001 
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
            chat = new LLamaSharpChatCompletion(ex);
#pragma warning disable SKEXP0001 
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001 
            Info("Using llama.cpp embedded library at {0} with model {1}", runtimePath, model);
        }
        else
        {
#pragma warning disable SKEXP0010 
            chat = new OpenAIChatCompletionService(model, new Uri(runtimePath), loggerFactory: loggerFactory);
#pragma warning restore SKEXP0010 
#pragma warning disable SKEXP0001
            client = chat.AsChatClient();
#pragma warning restore SKEXP0001

            promptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = 0.1f,
                ModelId = model,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true), 
                ExtensionData = new Dictionary<string, object>()
            };
            Info("Using OpenAI compatible API at {0} with model {1}", runtimePath, model);
        }
        
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(builder =>
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddProvider(loggerProvider)
            );
        builder.Services.AddChatClient(client).UseFunctionInvocation(loggerFactory);
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

    public ModelConversation AddChatPlugin<T>(string pluginName)
    {
        kernel.Plugins.AddFromType<T>(pluginName);
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

    public ChatCompletionAgent CreateAgent(string instructions, string name = "Default Agent")
    {
        return new ChatCompletionAgent()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel,
            LoggerFactory = loggerFactory,
            Arguments = new KernelArguments(promptExecutionSettings)
        };
    }   

    public AgentConversation CreateAgentConversation(string instructions, string name = "Default Agent", KernelArguments? arguments = null)
    {
        return new AgentConversation(this.runtimeType, this.model, instructions, name, this.runtimePath);
    }   
    #endregion

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

public class AgentConversation : ModelConversation
{
    public AgentConversation(ModelRuntime runtimeType, string model, string instructions, string name = "Default Agent", string runtimePath = "http://localhost:11434", string[]? systemPrompts = null) 
                : base(runtimeType, model, runtimePath, systemPrompts)
    {
        agent = new ChatCompletionAgent()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel,
            LoggerFactory = loggerFactory,
            Arguments = new KernelArguments(promptExecutionSettings),
        };

        options = new AgentInvokeOptions()
        {
            OnIntermediateMessage = (message) =>
            {
                if (message.Content is not null && !string.IsNullOrEmpty(message.Content))
                {
                    Info(message.ToString());   
                }
                return Task.CompletedTask;
            },  
        };  
    }

    public async new IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> Prompt(string prompt, params object[] args)
    {
        messages.AddUserMessage(string.Format(prompt, args));
        await foreach(var m in agent.InvokeAsync(messages, options: options))
        {
            messages.Add(m);    
            yield return m;
        }
           
    }
    protected ChatCompletionAgent agent;

    protected AgentInvokeOptions options;
    
}