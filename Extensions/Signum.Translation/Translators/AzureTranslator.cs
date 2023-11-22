using Signum.Utilities.Synchronization;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Signum.Translation.Translators;

// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-translate
public class AzureTranslator : ITranslator
{
    public string Name => "Azure";

    public Func<string?> AzureKey;
    public Func<string?>? Region;
    public Func<string?>? Proxy { get; set; }

    public AzureTranslator(Func<string?> azureKey, Func<string?>? region = null, Func<string?>? proxy = null)
    {
        AzureKey = azureKey;
        Region = region;
        Proxy = proxy;
    }

    public async Task<List<string?>?> TranslateBatchAsync(List<string> list, string from, string to)
    {
        var azureKey = AzureKey();

        if (string.IsNullOrEmpty(azureKey))
        {
            return null;
        }

        object[] body = list.Select(s => new { Text = s }).ToArray();
        var text = JsonSerializer.Serialize(body);

        using (var client = ExtendedHttpClient.GetClientWithProxy(Proxy?.Invoke()))
        using (var request = new HttpRequestMessage())
        {
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri($"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={from}&to={to}");
            request.Content = new StringContent(text, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", azureKey);
            var region = Region?.Invoke();
            if (region.HasText())
                request.Headers.Add("Ocp-Apim-Subscription-Region", region);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsByteArrayAsync();
            TranslationResult[] deserializedOutput = JsonSerializer.Deserialize<TranslationResult[]>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            return deserializedOutput.Select(a => (string?)a.Translations.Single().Text).ToList();
        }
    }

    public List<string?>? TranslateBatch(List<string> list, string from, string to)
    {
        if (AzureKey() == null)
            return null;

        var result = list.Chunk(10).SelectMany(listPart => Task.Run(async () =>
        {
            return await TranslateBatchAsync(listPart.ToList(), from, to);
        }).ResultSafe()!).ToList();

        return result;
    }

    public bool AutoSelect() => true;
}


public static class ExtendedHttpClient
{
    public static HttpClient GetClientWithProxy(string? proxy)
    {
        HttpClient client;
        if (!string.IsNullOrEmpty(proxy))
        {
            HttpClientHandler handler = new HttpClientHandler() { Proxy = new WebProxy() { Address = new Uri(proxy) } };
            client = new HttpClient(handler);
        }
        else
        {
            client = new HttpClient();
        }

        return client;
    }
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
/// <summary>
/// The C# classes that represents the JSON returned by the Translator Text API.
/// </summary>
public class TranslationResult
{
    public DetectedLanguage DetectedLanguage { get; set; }
    public TextResult SourceText { get; set; }
    public Translation[] Translations { get; set; }
}

public class DetectedLanguage
{
    public string Language { get; set; }
    public float Score { get; set; }
}

public class TextResult
{
    public string Text { get; set; }
    public string Script { get; set; }
}

public class Translation
{
    public string Text { get; set; }
    public TextResult Transliteration { get; set; }
    public string To { get; set; }
    public Alignment Alignment { get; set; }
    public SentenceLength SentLen { get; set; }
}

public class Alignment
{
    public string Proj { get; set; }
}

public class SentenceLength
{
    public int[] SrcSentLen { get; set; }
    public int[] TransSentLen { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

