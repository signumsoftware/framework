using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;

namespace Signum.Agent.Providers;

public class AnthropicProvider : IChatbotModelProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string apiKey = GetApiKey();

        var models = await new AnthropicClient(apiKey).Models.ListModelsAsync(ctx: ct);
        return models.Models.Select(models => models.Id).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();

        IChatClient client = new AnthropicClient(new APIAuthentication(apiKey)).Messages;

        return client;
    }

    public void CustomizeMessagesAndOptions(List<ChatMessage> messages, ChatOptions options)
    {
        var systemMessages = messages
            .Where(m => m.Role == ChatRole.System)
            .Select(m => new SystemMessage(string.Concat(m.Contents.OfType<TextContent>().Select(tc => tc.Text)))
            {
                CacheControl = new CacheControl { Type = CacheControlType.ephemeral }
            })
            .ToList();

        if (systemMessages.Count == 0)
            return;

        // Remove system messages from the list so ChatClientHelper doesn't re-add them without CacheControl
        messages.RemoveAll(m => m.Role == ChatRole.System);

        options.RawRepresentationFactory = _ => new MessageParameters { System = systemMessages };
    }

    private static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().AnthropicAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");
        return apiKey;
    }
}
