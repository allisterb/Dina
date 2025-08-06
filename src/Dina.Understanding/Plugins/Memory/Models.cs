using System.Collections.Generic;
using Microsoft.KernelMemory;

namespace Dina;

/// <summary>
/// A simplified wrapper for Microsoft.KernelMemory.SearchResult, suitable for serialization and user-facing output.
/// </summary>
public class SimpleSearchResult
{
    public List<SimpleSearchResultItem> Results { get; set; } = new();

    public SimpleSearchResult() { }

    public SimpleSearchResult(SearchResult searchResult)
    {
        if (searchResult?.Results != null)
        {
            foreach (var result in searchResult.Results)
            {
                // Use the first partition if available
                var partition = result.Partitions?.Count > 0 ? result.Partitions[0] : null;
                Results.Add(new SimpleSearchResultItem
                {
                    DocumentId = result.DocumentId ?? string.Empty,
                    Text = partition?.Text ?? string.Empty,
                    SourceName = result.SourceName,
                      
                });
            }
        }
    }

    public class SimpleSearchResultItem
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double? Relevance { get; set; }
        public string? SourceName { get; set; }
        public string? PartitionId { get; set; }
    }
}
