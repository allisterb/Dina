namespace Dina;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class FilesPlugin
{
    private readonly DirectoryInfo dir;

    public FilesPlugin(string dirpath)
    {
        dir = new DirectoryInfo(dirpath);
    }

    [KernelFunction, Description("Search for files by name pattern in the directory and its subdirectories")]
    public Task<List<string>> SearchFilesByNameAsync(
        [Description("File name pattern to search for, e.g. '*.txt' or 'report'")] string pattern)
    {
        var files = dir.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(f => f.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.FullName)
            .ToList();
        return Task.FromResult(files);
    }

    [KernelFunction, Description("Search for files containing the specified text in the directory and its subdirectories")]
    public async Task<List<string>> SearchFilesByContentAsync(
        [Description("Text to search for within files")] string searchText,
        ILogger logger)
    {
        var result = new List<string>();
        foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var content = "";
            try
            {
                if (file.Extension == ".pdf")
                {
                    content = Documents.ConvertPdfToText(file.FullName).Value.FirstOrDefault() ?? "";

                }
                else if (file.Extension == ".txt" || file.Extension == ".md")
                {
                    content = await File.ReadAllTextAsync(file.FullName);
                }
                else
                {
                    logger?.LogError("Unsupported file type: {FileName}", file.FullName);
                    continue;
                }
                    
                if (content.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(file.FullName);
                }
            }
            catch(Exception ex)
            {
                // Log the error and continue processing other files
                logger?.LogError(ex, "Error reading file: {FileName}", file.FullName);
            }
        }
        return result;
    }

    [KernelFunction, Description("Check if a file contains the specified text. Supports .pdf, .txt, and .md files.")]
    public async Task<bool> FileContainsTextAsync(
        [Description("Full path to the file to search")] string filePath,
        [Description("Text to search for within the file")] string searchText,
        ILogger logger
    )
    {
        var content = "";
        try
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
                return false;

            if (file.Extension == ".pdf")
            {
                content = Documents.ConvertPdfToText(file.FullName).Value.FirstOrDefault() ?? "";
            }
            else if (file.Extension == ".txt" || file.Extension == ".md")
            {
                content = await File.ReadAllTextAsync(file.FullName);
            }
            else
            {
                // Unsupported file type
                logger?.LogError("Unsupported file type: {FileName}", filePath);
                return false;
            }

            return content.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }
        catch(Exception ex)
        {
            // Log the error and return false
            logger?.LogError(ex, "Error reading file: {FileName}", filePath);
            
            return false;
        }
    }
}