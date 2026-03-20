using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Signum.Agent.Providers;

public class GithubModelsProvider : IChatbotModelProvider, IEmbeddingsProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        var allModels = await GetAllModelNames(ct);
        return allModels.Where(name => !name.Contains("embed", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<string>> GetEmbeddingModelNames(CancellationToken ct)
    {
        var allModels = await GetAllModelNames(ct);
        return allModels.Where(name => name.Contains("embed", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    async Task<List<string>> GetAllModelNames(CancellationToken ct)
    {
        var url = "https://models.github.ai/catalog/models";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        var doc = JsonDocument.Parse(json);

        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()!).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://models.github.ai/inference")
        };

        var token = GetToken();

        var client = new ChatClient(model.Model, new ApiKeyCredential(token), openAIOptions);

        return client.AsIChatClient();

    }

    public async Task<List<float[]>> GetEmbeddings(string[] inputs, EmbeddingsLanguageModelEntity model, CancellationToken ct)
    {
        var token = GetToken();
        var url = "https://models.github.ai/inference/embeddings";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        var requestBody = new
        {
            model = model.Model,
            input = inputs
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);

        var data = doc.RootElement.GetProperty("data");
        var result = data.EnumerateArray()
            .Select(e => e.GetProperty("embedding")
                .EnumerateArray()
                .Select(v => (float)v.GetDouble())
                .ToArray())
            .ToList();

        return result;
    }

    static string GetToken()
    {
        var apiKey = ChatbotLogic.GetConfig().GithubModelsToken;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No Token for Github Models configured!");
        return apiKey;
    }
}
