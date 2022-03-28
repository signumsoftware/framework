using DeepL;

namespace Signum.Engine.Translation;

public class DeepLTranslator : ITranslator
{
    public string Name => "DeepL";

    public Func<string?> DeepLApiKey;

    public Func<string?>? Proxy { get; }

    public DeepLTranslator(Func<string?> deepLKey)
    {
        this.DeepLApiKey = deepLKey;
    }

    List<SupportedLanguage>? supportedLanguages;

    public async Task<List<string?>?> TranslateBatchAsync(List<string> list, string from, string to)
    {
        var apiKey = DeepLApiKey();

        if(string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        using (DeepLClient client = new DeepLClient(apiKey))
        {
            if (supportedLanguages == null)
                supportedLanguages = (await client.GetSupportedLanguagesAsync()).ToList();

            if (!supportedLanguages.Any(a => a.LanguageCode == to.ToUpper()))
            {
                return null;
            }

            var translation = await client.TranslateAsync(list, sourceLanguageCode: from.ToUpper(), targetLanguageCode: to.ToUpper());

            return translation.Select(a => (string?)a.Text).ToList();
        }
    }

    public bool AutoSelect() => true;

    public List<string?>? TranslateBatch(List<string> list, string from, string to) 
    {
        var result = Task.Run(async () =>
        {
            return await this.TranslateBatchAsync(list, from, to);
        }).Result;

        return result;
    }
}

   
