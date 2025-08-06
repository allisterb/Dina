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

namespace Dina
{
    public class MemoryPlugin : IPlugin
    {
        Memory memory;
        IKernelMemory kernelMemory;
        string defaultIndex;
        bool waitForIngestionToComplete;
        /// <summary>
        /// Max time to wait for ingestion completion when <see cref="_waitForIngestionToComplete"/> is set to True.
        /// </summary>
        private readonly TimeSpan _maxIngestionWait = TimeSpan.FromSeconds(15);

        public MemoryPlugin(Memory memory,string defaultIndex = "", bool waitForIngestionToComplete = false)
        {
            this.memory = memory;
            this.kernelMemory = memory.memory;
            this.defaultIndex = defaultIndex;
            this.waitForIngestionToComplete= waitForIngestionToComplete;
        }

        [KernelFunction, Description("Search employee knowledge base for N documents")]
        public async Task<SimpleSearchResult> SearchKBAsync(
            [Description("The text to query the knowledge base with")]
            string query,
            [Description("The number of documents to return")]
            int limit = 1
        )
        {
            SearchResult result = await this.kernelMemory
               .SearchAsync(
                   query: query,
                   index: "kb"
                   //filter: TagsToMemoryFilter(tags ?? this._defaultRetrievalTags),
                   //minRelevance: minRelevance,
                   //limit: limit,
                   //cancellationToken: cancellationToken
                   );

            return new SimpleSearchResult(await memory.SearchAsync(query, "kb", limit: limit));
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
                while (!await this.kernelMemory.IsDocumentReadyAsync(documentId: documentId, cancellationToken: linkedTokenSource.Token).ConfigureAwait(false))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), linkedTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                // Nothing to do
            }
        }

        public Dictionary<string, Dictionary<string, object>> SharedState { get; set; } = new Dictionary<string, Dictionary<string, object>>();

    }
}
