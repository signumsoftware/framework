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

    List<GlossaryLanguagePair>? supportedLanguages;

    public async Task<List<string?>?> TranslateBatchAsync(List<string> list, string from, string to)
    {
        var apiKey = DeepLApiKey();

        if(string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        using (Translator translator = new Translator(apiKey))
        {
            if (supportedLanguages == null)
                supportedLanguages = (await translator.GetGlossaryLanguagesAsync()).ToList();

            if (!supportedLanguages.Any(a => a.TargetLanguageCode == to.ToLower()))
            {
                return null;
            }

            if (!supportedLanguages.Any(a => a.SourceLanguageCode == from.ToLower()))
            {
                return null;
            }

            var translation = await translator.TranslateTextAsync(list, sourceLanguageCode: from.ToLower(), targetLanguageCode: to.ToLower());

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

   
