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
    public AgentManager(ModelRuntime modelRuntime = ModelRuntime.Ollama,
        string textModel = OllamaModels.Gemma3n_e4b_tools_test, 
        string embeddingModel = OllamaModels.Nomic_Embed_Text,
        string endpointUrl = "http://localhost:11434")
    {
        this.modelRuntime = modelRuntime;
        this.textModel = textModel;
        this.embeddingModel = embeddingModel;
        this.endpointUrl = endpointUrl;
        config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: false)
            .Build();
        
        memory = new Memory(modelRuntime, textModel, embeddingModel, endpointUrl);
        sharedState["Config"] = new();
        email = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
        emailpassword = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
        emailDisplayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
        me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:DisplayName");
        homedir = config["Files:HomeDir"] ?? throw new ArgumentNullException("Files:HomeDir");
        kbdir = config["Files:KBDir"] ?? throw new ArgumentNullException("Files:KBDir");
        sharedState["Config"]["ManagerEmail"] = me;
        sharedState["Config"]["HomeDir"] = homedir;
    }

    public async Task CreateKBAsync()
    {
        var op = Begin("Indexing documents in KB dir {0}.", kbdir);
        await memory.CreateKBAsync(kbdir);
        op.Complete();
    }

    public AgentConversation StartUserSession()
    {
        var c = new AgentConversation("The user has just started the Dina program. You must help them get acclimated and answer any questions about Dina they may have.", "Startup Agent", 
            modelRuntime: modelRuntime, model: textModel, embeddingModel: embeddingModel, endpointUrl: endpointUrl,
            plugins: [
            //(new StatePlugin() {SharedState = sharedState}, "State"),
            (memory.plugin, "Memory"),
            (new MailPlugin(email, emailpassword, emailDisplayName) {SharedState = sharedState}, "Mail"),
            (new DocumentsPlugin(){SharedState = sharedState}, "Documents"),
            //(new ContactsPlugin() {SharedState = sharedState}, "Contacts"),
        ],
        systemPrompts: systemPrompts)
        {
            SharedState = sharedState 
        };
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
        "You are working for Dina, a document intelligence agent that assists blind users with getting information from printed and electronic documents and using this information to interface with different business systems and processes. " +
        "Your users are employees who are vision-impaired so keep your answers as short and precise as possible." + 
        "Your main role is to work on business documents at a console. Only one file at a time will be active in the console." +
        "ONLY use function calls to respond to the user's query on files and documents. If you do not know the answer the inform the user.",
        ];

    ModelRuntime modelRuntime;
    string textModel, embeddingModel, endpointUrl;
    string email, emailpassword, emailDisplayName, me, homedir, kbdir;
    
    #endregion
}

