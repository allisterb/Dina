using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dina.Understanding;

public static class Extensions
{
    /*
    [Experimental("SKEXP0010")]
    public static async IAsyncEnumerable<StreamingChatMessageContent> AddStreamingMessageAsync(this ChatHistory chatHistory, IAsyncEnumerable<StreamingChatMessageContent> streamingMessageContents, bool includeToolCalls = false)
    {
        List<StreamingChatMessageContent> messageContents = new List<StreamingChatMessageContent>();
        StringBuilder contentBuilder = null;
        Dictionary<int, string> toolCallIdsByIndex = null;
        Dictionary<int, string> functionNamesByIndex = null;
        Dictionary<int, StringBuilder> functionArgumentBuildersByIndex = null;
        Dictionary<string, object?>? metadata = null;
        AuthorRole? streamedRole = null;
        string streamedName = null;
        await foreach (StreamingChatMessageContent item in streamingMessageContents.ConfigureAwait(continueOnCapturedContext: false))
        {
            if (metadata == null)
            {
                metadata = item.Metadata;
            }

            string content = item.Content;
            if (content != null && content.Length > 0)
            {
                StringBuilder stringBuilder = contentBuilder;
                if (stringBuilder == null)
                {
                    StringBuilder stringBuilder2;
                    contentBuilder = (stringBuilder2 = new StringBuilder());
                    stringBuilder = stringBuilder2;
                }

                stringBuilder.Append(content);
            }

            //if (includeToolCalls)
            //{
              //  OpenAIFunctionToolCall.TrackStreamingToolingUpdate(item.ToolCallUpdates, ref toolCallIdsByIndex, ref functionNamesByIndex, ref functionArgumentBuildersByIndex);
            //}

            AuthorRole? authorRole = streamedRole;
            if (!authorRole.HasValue)
            {
                streamedRole = item.Role;
            }

            if (streamedName == null)
            {
                streamedName = item.AuthorName;
            }

            messageContents.Add(item);
            yield return item;
        }

        if (messageContents.Count != 0)
        {
            AuthorRole role = streamedRole ?? AuthorRole.Assistant;
            chatHistory.Add(new ChatMessageContent(role, contentBuilder?.ToString() ?? string.Empty, messageContents[0].ModelId, includeToolCalls ? ((IReadOnlyList<ChatToolCall>)OpenAIFunctionToolCall.ConvertToolCallUpdatesToFunctionToolCalls(ref toolCallIdsByIndex, ref functionNamesByIndex, ref functionArgumentBuildersByIndex)) : ((IReadOnlyList<ChatToolCall>)Array.Empty<ChatToolCall>()), metadata)
            {
                AuthorName = streamedName
            });
        }
    }
    */
}

