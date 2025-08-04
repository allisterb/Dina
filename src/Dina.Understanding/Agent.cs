namespace Dina;

using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public class Agent : Runtime
{
    public List<AgentConversation> Conversations = new List<AgentConversation>();

    public Dictionary<string, Dictionary<string, object>> SharedState { get; } = new()
    {
        { "Agent", new() }
    };

    public Agent()
    {
        config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: true)
            .Build();

        user = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
        password = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
        displayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
        me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:DisplayName");
        home = config["Files:HomeDir"] ?? throw new ArgumentNullException("Email:HomeDir");
    }

    public AgentConversation StartUserSession()
    {
        var c = new AgentConversation("The user has just started the Dina program. You must help them get acclimated and answer any questiond about Dina they may have.", "Startup Agent", plugins: [           
            (new StatePlugin() {SharedState = SharedState}, "State"),
            (new MailPlugin(user, password, displayName) {SharedState = SharedState}, "Mail"),
            (new DocumentsPlugin(){SharedState = SharedState}, "Documents"),
            (new FilesPlugin(home) {SharedState = SharedState}, "Files"),
        ],
        systemPrompts: systemPrompts);

        return c;
    }

    IConfigurationRoot config;

    string[] systemPrompts = [
        "You are Dina, a document intelligence agent that assists blind users with getting information from printed and electronic documents and interfacing with different business systems and processes. " +
        "Your users are employees who vision-impaired so keep your answers as short and precise as possible." +
        "Use only the provided tools to answer the user's queries and carry out actions they requested."
        ];

    string user, password, displayName, me, home;
}

