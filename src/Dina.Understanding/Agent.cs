namespace Dina;

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Functions;

public class AgentConversation : ModelConversation
{
    public AgentConversation(ModelRuntime runtimeType, string model, string instructions, string name = "Default Agent", bool immutableKernel = false,
        string runtimePath = "http://localhost:11434", string[]? systemPrompts = null, params (object, string)[] plugins)
                : base(runtimeType, model, runtimePath, systemPrompts)
    {
        agent = new ChatCompletionAgent()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel,
            LoggerFactory = loggerFactory,
#pragma warning disable SKEXP0130, SKEXP0001
            Arguments = new(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true,
                options: new FunctionChoiceBehaviorOptions()
                {
                    RetainArgumentTypes = true
                })
            }),
            UseImmutableKernel = immutableKernel
        };
        AddPlugins(plugins);
#pragma warning restore SKEXP0130, SKEXP0001
    }

    public new AgentConversation AddPlugin<T>(string pluginName)
    {
        kernel.Plugins.AddFromType<T>(pluginName);
        return this;
    }

    public new AgentConversation AddPlugin<T>(T plugin, string pluginName)
    {
#pragma warning disable SKEXP0120 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        kernel.Plugins.AddFromObject(plugin, jsonSerializerOptions: new System.Text.Json.JsonSerializerOptions(), pluginName: pluginName);
#pragma warning restore SKEXP0120 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return this;
    }

    public AgentConversation AddPlugins(params (object, string)[] plugins)
    {
        foreach (var (plugin, pluginName) in plugins)
        {
            kernel.Plugins.AddFromObject(plugin, pluginName);
        }
        return this;
    }

    public async new IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> Prompt(string prompt, params object[] args)
    {
        AgentThread? agentThread = null;
        bool hasThreads = agentThreads.TryPeek(out agentThread);
        await foreach (var m in agent.InvokeAsync(string.Format(prompt, args), agentThread))
        {
            if (!hasThreads || (hasThreads && agentThreads.Peek() != m.Thread))
            {
                agentThreads.Push(m.Thread);    
            }
            yield return m;
        }
    }

    public async Task TestAgent()
    {
        // Create an embedding generator for function vectorization
        // Create the agent thread and register the contextual function provider
        ChatHistoryAgentThread agentThread = new();

#pragma warning disable SKEXP0110,SKEXP0130 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        agentThread.AIContextProviders.Add(
            new ContextualFunctionProvider(
                vectorStore: vectorStore,
                vectorDimensions: 384,
                functions: GetAvailableFunctions(),
                maxNumberOfFunctions: 3, // Only the top 3 relevant functions are advertised
                loggerFactory: loggerFactory
            )
        );
#pragma warning restore SKEXP0110,SKEXP0130 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


        // Invoke the agent
        await foreach (var m in agent.InvokeAsync("Get and summarize customer reviews.", agentThread))
        {
            Console.WriteLine(m.Message.ToString());
        }

        // Output
        /*
            Customer Reviews:
            -----------------
            1. John D. - ★★★★★
               Comment: Great product and fast shipping!
               Date: 2023-10-01

            Summary:
            --------
            The reviews indicate high customer satisfaction,
            highlighting product quality and shipping speed.

            Available functions:
            --------------------
            - Tools-GetCustomerReviews
            - Tools-Summarize
            - Tools-CollectSentiments
        */

        IReadOnlyList<AIFunction> GetAvailableFunctions()
        {
            // Only a few functions are directly related to the prompt; the majority are unrelated to demonstrate the benefits of contextual filtering.
            return new List<AIFunction>
    {
        // Relevant functions
        AIFunctionFactory.Create(() => "[ { 'reviewer': 'John D.', 'date': '2023-10-01', 'rating': 5, 'comment': 'Great product and fast shipping!' } ]", "GetCustomerReviews"),
        AIFunctionFactory.Create((string text) => "Summary generated based on input data: key points include customer satisfaction.", "Summarize"),
        AIFunctionFactory.Create((string text) => "The collected sentiment is mostly positive.", "CollectSentiments"),

        // Irrelevant functions
        AIFunctionFactory.Create(() => "Current weather is sunny.", "GetWeather"),
        AIFunctionFactory.Create(() => "Email sent.", "SendEmail"),
        AIFunctionFactory.Create(() => "The current stock price is $123.45.", "GetStockPrice"),
        AIFunctionFactory.Create(() => "The time is 12:00 PM.", "GetCurrentTime")
    };
        }
    }
    #region Fields
    protected ChatCompletionAgent agent;
    protected Stack<AgentThread> agentThreads = new Stack<AgentThread>();   
    #endregion
}
