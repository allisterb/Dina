namespace Dina;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

public class DocumentsPlugin : IPlugin
{
    public Dictionary<string, Dictionary<string, object>> SharedState { get; set; } = new Dictionary<string, Dictionary<string, object>>();

    [KernelFunction, Description("Open a file document at a particular location and set it as the active document.")]
    public async Task<string> OpenFileDocument(
        [Description("The file path to open.")] string filePath,
        ILogger? logger
    )
    {
        string homedir = (string)SharedState["Config"]["HomeDir"];
        if (File.Exists(filePath) || File.Exists(Path.Combine(homedir, filePath)))
        {
            var path = File.Exists(filePath) ? filePath : Path.Combine(homedir, filePath);
            var text = await Documents.GetDocumentText(path); 
            if (!string.IsNullOrEmpty(text))
            {
                ActiveDocuments.Push((path, text));
                return $"The current active file is  \"{path}\"";
            }
            else
            {
                return """
                    The file content could not be read. You can only read certain file types like PDF files, 
                    MS and OpenOffice documents, and scanned document images. Ask the user if they want to reenter the path
                    or choose something else to do.
                    """;            
            }
        }
        else
        {
            return $"The file does not exist. Ask the user to enter the path again, or choose something else to do.";
        }
    }


    [KernelFunction, Description("Scan a document using the attached scanner and set it as the active document.")]
    public async Task<string> ScanDocument()
    {
        var text = await Documents.ScanTextAsync();
        if (text.IsSuccess)
        {
            ActiveDocuments.Push(("Scanned_" + DateTime.Now.ToString(), text.Value));
            return $"The file was scanned and is now the active document.";
        }
        else
        {
            //logger?.LogError(text.Message, text.Exception);
            return "You weren't able to scan the file due to an error. Ask the user if they would like to try again or pick something else to do";
        }

    }

    [KernelFunction, Description("Get the text of the current document for answering questions and analysis.")]
    public string GetCurrentDocumentText(
      ILogger? logger
    )
    {
        if (ActiveDocuments.TryPeek(out var document))
        {
            return document.Item2;
        }
        else
            return
                """
                No document is currently active. Ask the user if they want to enter the path to a document, then open the document at that path.
                Or ask if they want to do something else.
                """;
    }
    
    /*
     *     [KernelFunction, Description("Get the path of the current document.")]
    public string GetCurrentDocumentPath(
       ILogger? logger
   )
    {
        if (ActiveDocuments.TryPeek(out var document))
        {
            return document.Item1;
        }
        else
            return
                """
                No document is currently active. Ask the user if they want to enter the path to a document, then open the document at that path.
                Or ask if they want to do something else.
                """;
    }

    [KernelFunction, Description("Get all of the text contained in a document file. Supports text, PDF and image files.")]
    public async Task<string> GetDocumentText(
       [Description("The path to the file")] string filePath,
       ILogger? logger)
    {
        try
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                logger?.LogError("File does not exist: {FilePath}", filePath);
                return "";
            }

            string ext = file.Extension.ToLowerInvariant();
            if (ext == ".pdf")
            {
                var result = Documents.ConvertPdfToText(file.FullName);
                if (result.IsSuccess)
                {
                    return string.Join("\n", result.Value);
                }
                else
                {
                    logger?.LogError(result.Message, result.Exception);
                    return "";
                }
            }
            else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".tiff")
            {
                var result = await Documents.OcrImageAsync(file.FullName);
                if (result.IsSuccess)
                {
                    return result.Value;
                }
                else
                {
                    logger?.LogError(result.Message, result.Exception);
                    return "";
                }
            }
            else
            {
                logger?.LogError("Unsupported file type: {FilePath}", filePath);
                return "";
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting document text for {FilePath}", filePath);
            return "";
        }

    }
    */

   
   

    public Stack<(string, string)> ActiveDocuments = new Stack<(string, string)>();
}