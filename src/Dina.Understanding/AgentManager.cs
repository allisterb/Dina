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
    public AgentManager()
    {
        config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: true)
            .Build();
        
        memory = new Memory(ModelRuntime.Ollama, OllamaModels.Gemma3n_4eb_tools, OllamaModels.Nomic_Embed_Text);
        sharedState["Config"] = new();
        email = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
        emailpassword = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
        emailDisplayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
        me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:DisplayName");
        homedir = config["Files:HomeDir"] ?? throw new ArgumentNullException("Files:HomeDir");
        kbdir = config["Files:KBDir"] ?? throw new ArgumentNullException("Files:KBDir");
        sharedState["Config"]["ManagerEmail"] = me;
    }

    public async Task IndexKBAsync()
    {
        var op = Begin("Indexing documents in KB dir {0}.", kbdir);

        /*
        foreach (var file in Directory.EnumerateFiles(homedir))
        {
            var text = await Documents.GetDocumentText(file);
            if (!string.IsNullOrEmpty(text))
            {
                await memory.plugin.SaveAsync(text, index:"home");
            }
        }*/
        await memory.plugin.SaveAsync("All employees must wash hands after meals.");
        op.Complete();
    }

    public AgentConversation StartUserSession()
    {
        var c = new AgentConversation("The user has just started the Dina program. You must help them get acclimated and answer any questions about Dina they may have.", "Startup Agent", plugins: [           
            (new StatePlugin() {SharedState = sharedState}, "State"),
            (new MailPlugin(email, emailpassword, emailDisplayName) {SharedState = sharedState}, "Mail"),
            (new DocumentsPlugin(){SharedState = sharedState}, "Documents"),
            (new FilesPlugin(homedir) {SharedState = sharedState}, "Files"),
        ],
        systemPrompts: systemPrompts);
        c.AddPlugin(memory.plugin, "KnowledgeBase");
        return c;
    }

    #region Fields
    public Dictionary<string, Dictionary<string, object>> sharedState = new()
    {
        { "Agent", new() }
    };

    public List<AgentConversation> conversations = new List<AgentConversation>();

    Memory memory;

    IConfigurationRoot config;

    string[] systemPrompts = [
        "You are Dina, a document intelligence agent that assists blind users with getting information from printed and electronic documents and using this information to interface with different business systems and processes. " +
        "Your users are employees who are vision-impaired so keep your answers as short and precise as possible." +
        "Use ONLY the provided tools to answer the user's queries and carry out actions they requested." 
        ];

    string email, emailpassword, emailDisplayName, me, homedir, kbdir;
    #endregion
}

