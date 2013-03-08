using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using Signum.Utilities;

namespace Signum.Web.Localization
{
    public interface ITranslator
    {
        List<string> TranslateBatch(List<string> list, string from, string to);
    }

    public class MockTranslator : ITranslator
    {
        public List<string> TranslateBatch(List<string> list, string from, string to)
        {
            return list.Select(text => "In{0}({1})".Formato(to, text)).ToList();
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
    //        LanguageServiceClient cliente = new LanguageServiceClient();
    //        TranslateArrayResponse[] result = cliente.TranslateArray(YourAppId, list.ToArray(), from, to, new TranslateOptions());

    //        return result.Select(a => a.TranslatedText).ToList();
    //    }
    //}
}
