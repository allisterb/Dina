namespace Dina;

using System.Linq;

using OllamaSharp;

public class Model : Runtime
{
    public static async Task<bool> Initialize()
    {
        if (!await ollama.IsRunningAsync())
        {
            Error("Ollama is not running. Please start the Ollama server first.");
            return false;   
        }
        var op = Begin("Downloading gemma3n:latest model. This only needs to be done once");
        await foreach (var status in ollama.PullModelAsync("gemma3n:latest"))
            Info("{0}% of {1} bytes complete.  Status: {2}", status.Percent, status.Total, status.Status);
        if ((await ollama.ListLocalModelsAsync()).Any(m => m.Name == "gemma3n:latest"))
        {
            op.Complete();
            ollama.SelectedModel = "gemma3n:latest";
            IsInitialized = true;
            Info("Model gemma3n:latest is ready to use.");
            return true;
        }
        else
        {
            Error("Failed to download model gemma3n:latest.");
            return false;
        }  
    }

    public static bool IsInitialized = false;
    
    public static Chat StartChat(string prompt) => new Chat(ollama, prompt);
    
    public static OllamaApiClient ollama = new OllamaApiClient("http://localhost:11434");

    
}
