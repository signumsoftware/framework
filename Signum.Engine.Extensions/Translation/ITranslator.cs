using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Engine.Translation
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

    public class EmptyTranslator : ITranslator
    {
        public List<string> TranslateBatch(List<string> list, string from, string to)
        {
            return list.Select(text => "").ToList();
        }

        public bool AutoSelect()
        {
            return false;
        }
    }

    public class MockTranslator : ITranslator
    {
        public List<string> TranslateBatch(List<string> list, string from, string to)
        {
            return list.Select(text => "In{0}({1})".FormatWith(to, text)).ToList();
        }

        public bool AutoSelect()
        {
            return false;
        }
    }

    public class AlreadyTranslatedTranslator : ITranslator
    {
        ITranslator Inner;

        public AlreadyTranslatedTranslator(ITranslator inner)
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));

            this.Inner = inner;
        }

        public List<string> TranslateBatch(List<string> list, string from, string to)
        {
            var alreadyTranslated = (from ass in AppDomain.CurrentDomain.GetAssemblies()
                                     let daca = ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>()
                                     where daca != null && daca.DefaultCulture == @from
                                     from trans in GetAllTranslations(ass, @from, to)
                                     group trans.Value by trans.Key into g
                                     let only = g.Distinct().Only()
                                     where only != null
                                     select KVP.Create(g.Key, only))
                                     .ToDictionary();

            var dic = list.Distinct().ToDictionary(l => l, l => alreadyTranslated.TryGetC(l));

            if (dic.Any(kvp => kvp.Value == null))
            {
                var subList = dic.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();

                var subResult = Inner.TranslateBatch(subList, from, to);

                dic.SetRange(subList, subResult);
            }

            return list.Select(dic.GetOrThrow).ToList();
        }

        private IEnumerable<KeyValuePair<string, string>> GetAllTranslations(Assembly assembly, string from, string to) 
        {
            var locFrom = DescriptionManager.GetLocalizedAssembly(assembly, CultureInfo.GetCultureInfo(from));
            var locTo = DescriptionManager.GetLocalizedAssembly(assembly, CultureInfo.GetCultureInfo(to));

            if (locFrom == null || locTo == null)
                return Enumerable.Empty<KeyValuePair<string, string>>();

            return (locFrom.Types.JoinDictionary(locTo.Types, (type, ft, tt)=>GetAllTranslations(ft, tt))).Values.SelectMany(pairs => pairs);
        }

        private IEnumerable<KeyValuePair<string, string>> GetAllTranslations(LocalizedType from, LocalizedType to)
        {
            if (from.Description.HasText() && to.Description.HasText())
                yield return KVP.Create(from.Description, to.Description);

            if (from.PluralDescription.HasText() && to.PluralDescription.HasText())
                yield return KVP.Create(from.PluralDescription, to.PluralDescription);

            foreach (var item in from.Members)
            {
                var toMember = to.Members.TryGetC(item.Key);

                if (toMember.HasText())
                    yield return KVP.Create(item.Value, toMember);
            }
        }

        public bool AutoSelect()
        {
            return true;
        }
    }

    public class ReplacerTranslator : ITranslatorWithFeedback
    {
        ITranslator Inner;

        public ReplacerTranslator(ITranslator inner)
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));

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


}
