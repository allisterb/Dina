namespace Dina;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

public class DocumentsPlugin : IPlugin
{
    public Dictionary<string, Dictionary<string, object>> SharedState { get; set; } = new Dictionary<string, Dictionary<string, object>>();

    [KernelFunction, Description("Scan a paper document using an attached scanner and return the text.")]
    public async Task<string> ScanDocument(ILogger? logger)
    {
        var text = await Documents.ScanTextAsync();
        if (text.IsSuccess)
        {
            return text.Value;
        }
        else
        {
            logger?.LogError(text.Message, text.Exception);
            return "";
        }
  
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
}