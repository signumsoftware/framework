using DeepL;
using DeepL.Model;

namespace Signum.Translation.Translators;

public class DeepLTranslator : ITranslator
{
    public string Name => "DeepL";

    public Func<string?> DeepLApiKey;

    public Func<string?>? Proxy { get; }

    public List<TargetLanguage> AdditionalTargetLanguages { get; }

    public DeepLTranslator(Func<string?> deepLKey, List<TargetLanguage> additionalTargetLanguages)
    {
        DeepLApiKey = deepLKey;
        AdditionalTargetLanguages = additionalTargetLanguages;
    }

    List<SourceLanguage>? sourceLanguages;
    List<TargetLanguage>? targetLanguage;

    public async Task<List<string?>?> TranslateBatchAsync(List<string> list, string from, string to)
    {
        from = NormalizeLanguage(from);
        to = NormalizeLanguage(to);

        var apiKey = DeepLApiKey();

        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        using (Translator translator = new Translator(apiKey))
        {
            if (sourceLanguages == null)
                sourceLanguages = (await translator.GetSourceLanguagesAsync()).ToList();

            if (targetLanguage == null)
            { 
                targetLanguage = (await translator.GetTargetLanguagesAsync()).ToList();

                if (AdditionalTargetLanguages.Any())
                    targetLanguage.AddRange(AdditionalTargetLanguages);
            }

            if (!sourceLanguages.Any(a => a.Code == from))
            {
                var fromNeutral = Neutral(from);
                var candidates = sourceLanguages.Where(a => Neutral(a) == fromNeutral);

                var best = BestCandidate(from, candidates);

                if (best == null)
                    return null;

                from = best.Code;
            }

            if (!targetLanguage.Any(a => a.Code == to))
            {
                var toNeutral = Neutral(to);
                var candidates = targetLanguage.Where(a => Neutral(a) == toNeutral);

                var best = BestCandidate(to, candidates);

                if (best == null)
                    return null;

                to = best.Code;
            }

            var translation = await translator.TranslateTextAsync(list.Where(l => !string.IsNullOrEmpty(l)), sourceLanguageCode: from, targetLanguageCode: to);

            return translation.Select(a => (string?)a.Text).ToList();
        }
    }

    private Func<string, IEnumerable<Language>, Language?> BestCandidate = (from, candidates) => candidates.FirstOrDefault();

    string Neutral(string lang)
    {
        return lang.TryBefore("-") ?? lang;
    }

    private string NormalizeLanguage(string langCode)
    {
        if (langCode.Length == 2)
            langCode = langCode.ToLower();


        return langCode;
    }

    public bool AutoSelect() => true;

    public List<string?>? TranslateBatch(List<string> list, string from, string to)
    {
        var result = Task.Run(async () =>
        {
            return await TranslateBatchAsync(list, from, to);
        }).Result;

        return result;
    }
}


