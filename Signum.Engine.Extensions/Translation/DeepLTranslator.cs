using DeepL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Engine.Translation
{
    public class DeepLTranslator : ITranslator
    {
        public Func<string?> DeepLApiKey;

        public ITranslator? fallbackTranslator;

        public Func<string?>? Proxy { get; }

        public DeepLTranslator(Func<string?> deepLKey, ITranslator? fallbackTranslator)
        {
            this.DeepLApiKey = deepLKey;
        }

        List<SupportedLanguage>? supportedLanguages;

        public async Task<List<string?>> TranslateBatchAsync(List<string> list, string from, string to)
        {
            var apiKey = DeepLApiKey();

            if(apiKey == null)
            {
                if (fallbackTranslator != null)
                    return fallbackTranslator.TranslateBatch(list, from, to);

                throw new Exception("Neither DeeplApiKey or fallbackTranslator set");
            }

            using (DeepLClient client = new DeepLClient(apiKey))
            {
                if (supportedLanguages == null)
                    supportedLanguages = (await client.GetSupportedLanguagesAsync()).ToList();

                if(! supportedLanguages.Any(a=>a.LanguageCode == to))
                {
                    if (fallbackTranslator != null)
                        return fallbackTranslator.TranslateBatch(list, from, to);

                    throw new Exception($"Translating to {to} is not supported by DeepL and no fallbackTranslator is set");
                }

                var translation = await client.TranslateAsync(list, sourceLanguageCode: from.ToUpper(), targetLanguageCode: to.ToUpper());

                return translation.Select(a => (string?)a.Text).ToList();
            }
        }

        public bool AutoSelect() => true;

        public List<string?> TranslateBatch(List<string> list, string from, string to) 
        {
            var result = Task.Run<List<string?>>(async () =>
            {
                return await this.TranslateBatchAsync(list, from, to);
            }).Result;

            return result;
        }
    }

   
}
