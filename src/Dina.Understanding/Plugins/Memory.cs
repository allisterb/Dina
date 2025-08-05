using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dina.Understanding.Plugins
{
    public class MemoryPlugin
    {
        IKernelMemory memory;
        string defaultIndex;
        bool waitForIngestionToComplete;
        /// <summary>
        /// Max time to wait for ingestion completion when <see cref="_waitForIngestionToComplete"/> is set to True.
        /// </summary>
        private readonly TimeSpan _maxIngestionWait = TimeSpan.FromSeconds(15);

        public MemoryPlugin(IKernelMemory memoryClient,string defaultIndex = "", bool waitForIngestionToComplete = false)
        {
            this.memory = memoryClient;
            this.defaultIndex = defaultIndex;
            this.waitForIngestionToComplete= waitForIngestionToComplete;
        }

        [KernelFunction, Description("Search employee knowledge base")]
        public async Task<string> SearchKBAsync(string query, int limit = 1)
        {
            SearchResult result = await this.memory
           .SearchAsync(
               query: query,
               index: "kb"
               //filter: TagsToMemoryFilter(tags ?? this._defaultRetrievalTags),
               //minRelevance: minRelevance,
               //limit: limit,
               //cancellationToken: cancellationToken
               );

            if (result.Results.Count == 0)
            {
                return string.Empty;
            }

            // Return the first chunk(s) of the relevant documents
            return limit == 1
                ? result.Results.First().Partitions.First().Text
                : JsonSerializer.Serialize(result.Results.Select(x => x.Partitions.First().Text));

        }

        private async Task WaitForDocumentReadinessAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (!this.waitForIngestionToComplete)
            {
                return;
            }

            using var timedTokenSource = new CancellationTokenSource(this._maxIngestionWait);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timedTokenSource.Token, cancellationToken);

            try
            {
                while (!await this.memory.IsDocumentReadyAsync(documentId: documentId, cancellationToken: linkedTokenSource.Token).ConfigureAwait(false))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), linkedTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                // Nothing to do
            }
        }

        private static MemoryFilter? TagsToMemoryFilter(TagCollection? tags)
        {
            if (tags == null)
            {
                return null;
            }

            var filters = new MemoryFilter();

            foreach (var tag in tags)
            {
                filters.Add(tag.Key, tag.Value);
            }

            return filters;
        }
    }
}
