using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using Signum.Engine.Translation;
using Signum.Entities.Translation;
using Signum.Utilities;

namespace Signum.Web.Translation
{
    public interface ITranslator
    {
        List<string> TranslateBatch(List<string> list, string from, string to);

        bool AutoSelect();
    }

    public interface ITranslatorWithFeedback: ITranslator
    {
        void Feedback(string to, string wrongTranslation, string fixedTranslation);
    }

    public class MockTranslator : ITranslator
    {
        public List<string> TranslateBatch(List<string> list, string from, string to)
        {
            return list.Select(text => "In{0}({1})".Formato(to, text)).ToList();
        }


        public bool AutoSelect()
        {
            return false;
        }
    }

    public class ReplacerTranslator : ITranslatorWithFeedback
    {
        ITranslator Inner;

        public ReplacerTranslator(ITranslator inner)
        {
            if (inner == null)
                throw new ArgumentNullException("inner");

            this.Inner = inner;
        }

        public List<string> TranslateBatch(List<string> list, string from, string to)
        {
            var result = Inner.TranslateBatch(list, from, to);

            TranslationReplacementPack pack = TranslationReplacementLogic.ReplacementsLazy.Value.TryGetC(CultureInfo.GetCultureInfo(to));

            if (pack == null)
                return result;

            return result.Select(s => s == null ? s : pack.Regex.Replace(s, m =>
            {
                string replacement = pack.Dictionary.GetOrThrow(m.Value);

                return IsUpper(m.Value) ? replacement.ToUpper() :
                    IsLower(m.Value) ? replacement.ToLower() :
                    char.IsUpper(m.Value[0]) ? replacement.FirstUpper() :
                    replacement.FirstLower();
            })).ToList();
        }


        bool IsUpper(string p)
        {
            return p.ToUpper() == p && p.ToLower() != p;
        }

        bool IsLower(string p)
        {
            return p.ToLower() == p && p.ToUpper() != p;
        }

        public void Feedback(string culture, string wrongTranslation, string fixedTranslation)
        {
            TranslationReplacementLogic.ReplacementFeedback(CultureInfo.GetCultureInfo(culture), wrongTranslation, fixedTranslation);
        }

        public bool AutoSelect()
        {
            return Inner.AutoSelect();
        }
    }

    //public class GoogleTranslator : ITranslator
    //{
    //    string Translate(string text, string from, string to)
    //    {
    //        string url = string.Format("http://translate.google.com/translate_a/t?client=t&text={0}&hl={1}&sl={1}&tl={2}", HttpUtility.UrlEncode(text), from, to);

    //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    //        request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; es-ES; rv:1.9.2.3) Gecko/20100401 Firefox/3.6.3";
    //        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    //        string response = null;
    //        using (StreamReader readStream = new StreamReader(request.GetResponse().GetResponseStream(), Encoding.UTF8))
    //        {
    //            response = readStream.ReadToEnd();
    //        }

    //        string result = serializer.Deserialize<googleResponse>(response).sentences[0].trans;
    //        result = Fix(result, text);
    //        return result;
    //    }

    //    public bool AutoSelect()
    //    {
    //        return true;
    //    }
     
    //    public class googleResponse
    //    {
    //        public List<googleSentence> sentences;
    //        public string src;
    //    }

    //    public class googleSentence
    //    {
    //        public string trans;
    //        public string orig;
    //        public string translit;
    //    }

    //    private string Fix(string result, string text)
    //    {
    //        return Regex.Replace(result, @"\(+\d\)+", m => m.Value.Replace("(", "{").Replace(")", "}"));
    //    }

    //    JavaScriptSerializer serializer = new JavaScriptSerializer();

    //    //Sample {"sentences":[{"trans":"hello that such \"these\" Jiminy Cricket","orig":"hola que tal \"estas\" pepito grillo","translit":""}],"src":"es"}"

    //    public override string ToString()
    //    {
    //        return "Google translate";
    //    }

    //    public List<string> TranslateBatch(List<string> list, string from, string to)
    //    {
    //        return list.Select(text => Translate(text, from, to)).ToList();
    //    }
    //}


    //public class BingTranslator : ITranslator
    //{
    //    public List<string> TranslateBatch(List<string> list, string from, string to)
    //    {
    //        string token = AdmAuthentication.GetAccessToken( "ClientId", "Secret"); //find one in https://datamarket.azure.com/developer/applications/register
    //        LanguageServiceClient client = new LanguageServiceClient();
    //        using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
    //        {
    //            System.ServiceModel.OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = new HttpRequestMessageProperty
    //            {
    //                Method = "POST",
    //                Headers = { { "Authorization", "Bearer " + token } }
    //            };

    //            return list.GroupsOf(a => a.Length, 10000).SelectMany(gr =>
    //            {
    //                TranslateArrayResponse[] result = client.TranslateArray("", gr.ToArray(), from, to, new TranslateOptions());

    //                return result.Select(a => a.TranslatedText).ToList();

    //            }).ToList();
    //        }
    //    }

    //    public bool AutoSelect()
    //    {
    //        return true;
    //    }

    //}

    public static class AdmAuthentication
    {
        [DataContract]
        public class AdmAccessToken
        {
            [DataMember]
            public string access_token { get; set; }
            [DataMember]
            public string token_type { get; set; }
            [DataMember]
            public string expires_in { get; set; }
            [DataMember]
            public string scope { get; set; }
        }

        public static string GetAccessToken(string clientId, string clientSecret)
        {
            //Prepare OAuth request 
            WebRequest webRequest = WebRequest.Create("https://datamarket.accesscontrol.windows.net/v2/OAuth2-13");
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com",
                HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret)));
            webRequest.ContentLength = bytes.Length;
            using (Stream outputStream = webRequest.GetRequestStream())
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
                //Get deserialized object from JSON stream
                AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
                return token.access_token;
            }
        }
    }
}
