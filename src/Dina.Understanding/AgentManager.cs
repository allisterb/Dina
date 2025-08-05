namespace Dina;

using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public class AgentManager : Runtime
{
    public List<AgentConversation> Conversations = new List<AgentConversation>();

    public Dictionary<string, Dictionary<string, object>> SharedState { get; } = new()
    {
        { "Agent", new() }
    };

    public AgentManager()
    {
        config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: true)
            .Build();
        memory = new Memory(ModelRuntime.Ollama, OllamaModels.Gemma3n_2eb_tools, OllamaModels.All_MiniLm);
        useremail = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
        useremailpassword = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
        emailDisplayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
        me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:DisplayName");
        homedir = config["Files:HomeDir"] ?? throw new ArgumentNullException("Email:HomeDir");
       
    }

    public AgentConversation StartUserSession()
    {
        var op = Begin("Indexing documents in home dir {0}.", homedir);
        Task.Run(async () =>
        {
            foreach (var file in Directory.EnumerateFiles(homedir))
            {
                var text = await Documents.GetDocumentText(file);
                if (!string.IsNullOrEmpty(text))
                await memory.ImportTextAsync(text, file, "home");
            }
        }).Wait();
        var c = new AgentConversation("The user has just started the Dina program. You must help them get acclimated and answer any questions about Dina they may have.", "Startup Agent", plugins: [           
            (new StatePlugin() {SharedState = SharedState}, "State"),
            (new MailPlugin(useremail, useremailpassword, emailDisplayName) {SharedState = SharedState}, "Mail"),
            (new DocumentsPlugin(){SharedState = SharedState}, "Documents"),
            (new FilesPlugin(homedir) {SharedState = SharedState}, "Files"),
        ],
        systemPrompts: systemPrompts);
        c.AddPlugin(memory.plugin, "memory");
        return c;
    }

    Memory memory;

    IConfigurationRoot config;

    string[] systemPrompts = [
        "You are Dina, a document intelligence agent that assists blind users with getting information from printed and electronic documents and using this information to interface with different business systems and processes. " +
        "Your users are employees who are vision-impaired so keep your answers as short and precise as possible." +
        "Use ONLY the provided tools to answer the user's queries and carry out actions they requested." +
        "If you don't know the answer to a question then just let the user know."
        ];

    string useremail, useremailpassword, emailDisplayName, me, homedir;
}

