namespace Dina;

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using static Result;

public class StatePlugin : IPlugin
{
    public Dictionary<string, Dictionary<string, object>> SharedState { get; set; } = new();

    [KernelFunction, Description("Get the goals of this conversation and the capabilities of this agent.")]
    public string[] GetGoals(
       ILogger? logger = null)
    {
        if (SharedState["Agent"].ContainsKey("Goals"))
        {
            return (string[])SharedState["Agent"]["Goals"];
        }
        else
        {
            logger?.LogError("Shared state doesn't contain Goals key.");
            return [];
        }
    }

    [KernelFunction, Description("Get a list of the hardware devices currently attached to this computer.")]
    public async Task<string[]> GetHardwareDevicesAsync(
        [Description("The hardware device category to list e.g scanners, cameras")] string category,
        ILogger? logger = null) => category switch
        {
            "scanners" => await Documents.GetScannerNames(),
            _ => []
        };
}