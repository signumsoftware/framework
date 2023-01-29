using DeepL;
using DeepL.Model;

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

    List<SourceLanguage>? sourceLanguages;
    List<TargetLanguage>? targetLanguage;

    public async Task<List<string?>?> TranslateBatchAsync(List<string> list, string from, string to)
    {
        from = NormalizeLanguage(from);
        to = NormalizeLanguage(to);

        var apiKey = DeepLApiKey();

        if(string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        using (Translator translator = new Translator(apiKey))
        {
            if (sourceLanguages == null)
                sourceLanguages = (await translator.GetSourceLanguagesAsync()).ToList();

            if (targetLanguage == null)
                targetLanguage = (await translator.GetTargetLanguagesAsync()).ToList();

            if (!sourceLanguages.Any(a => a.Code == from))
                return null;

            if (!targetLanguage.Any(a => a.Code == to))
                return null;

            var translation = await translator.TranslateTextAsync(list, sourceLanguageCode: from, targetLanguageCode: to);

            return translation.Select(a => (string?)a.Text).ToList();
        }
    }

    private string NormalizeLanguage(string langCode)
    {
        if (langCode.Length == 2)
            langCode = langCode.ToLower();

        if (langCode == "en")
            langCode = "en-GB";

        return langCode;
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

   
