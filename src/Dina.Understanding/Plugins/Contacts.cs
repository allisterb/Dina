namespace Dina;

using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using static Result;

public class ContactsPlugin : IPlugin
{
    public Dictionary<string, Dictionary<string, object>> SharedState { get; set; } = new();

    [KernelFunction, Description("Get the email of an employee of this firm.")]
    public string GetEmployeeEmail(
        [Description("The name of the employee")]
        string name,
        ILogger? logger = null) => name switch
        {
            "John Smiley" => (string)SharedState["Config"]["ManagerEmail"],
            _ => ""
        };
}