using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Utilities.NaturalLanguage
{
    public class GermanPluralizer : IPluralizer
    {
        //http://www.alemansencillo.com/el-plural-en-aleman#TOC-Reglas-generales-aplicables-a-todos
        Dictionary<string, string> terminationsFemenine = new Dictionary<string, string>
        {
            {"itis", "itiden"},
            {"sis", "sen"},
            {"xis", "xien"},
            {"in", "innen"},
            {"aus", "äuse"},
            {"e", "en"},
            {"a", "en"},
            {"", "en"},
        };

        Dictionary<string, string> terminationsMasculine = new Dictionary<string, string>
        {
            {"ant", "anten"},
            {"ent", "enten"},
            {"ist", "isten"},
            {"at", "aten"},
            {"us", "usse"},
            {"e", "en"},
            {"", "e"},
        };

        Dictionary<string, string> terminationsNeutro = new Dictionary<string, string>
        {
            {"nis", "nisse"},
            {"um", "a"},
            {"o", "en"},
            {"", "e"},
        };


        public string MakePlural(string singularName)
        {
            if (string.IsNullOrEmpty(singularName))
                return singularName;

            string last = singularName.TryAfterLast(' ');
            if (last != null)
                return singularName.BeforeLast(' ') + " " + MakePlural(last);

            var gender  = NaturalLanguageTools.GenderDetectors["de"].GetGender(singularName);

            var dic = gender == 'f' ? terminationsFemenine :
                gender == 'm' ? terminationsMasculine :
                gender == 'n' ? terminationsNeutro : null;

            if (dic == null)
                return singularName;

            var result = dic.FirstOrDefault(r => singularName.EndsWith(r.Key));
            if (result.Value != null)
                return singularName.Substring(0, singularName.Length - result.Key.Length) + result.Value;

            return singularName;
        }
    }

    public class GermanGenderDetector : IGenderDetector
    {
        Dictionary<string, char> terminations = new Dictionary<string, char>
        {
            //http://www.alemansencillo.com/genero-de-los-sustantivos-masculinos
            {"ich", 'm' },
            {"ist", 'm' },
            {"or", 'm' },
            {"ig", 'm' },
            {"ling", 'm' },
            {"ismus", 'm' },
            {"ant", 'm' },
            {"är", 'm' },
            {"eur", 'm' },
            {"iker", 'm' },
            {"ps", 'm' },

            //http://www.alemansencillo.com/genero-de-los-sustantivos-masculinos
            {"ei", 'f' },
            {"ung", 'f' },
            {"in", 'f' },
            {"heit", 'f' },
            {"keit", 'f' },
            {"ion", 'f' },
            {"ie", 'f' },
            {"schaft", 'f' },
            {"elle", 'f' },
            {"ik", 'f' },
            {"ur", 'f' },
            {"ade", 'f' },
            {"age", 'f' },
            {"ette", 'f' },
            {"enz", 'f' },
            {"ere", 'f' },
            {"ine", 'f' },
            {"isse", 'f' },
            {"tät", 'f' },
            {"itis", 'f' },
            {"ive", 'f' },
            {"se", 'f' },
            {"sis", 'f' },

            //http://www.alemansencillo.com/genero-de-los-sustantivos-neutros
            {"chen", 'n' },
            {"lein", 'n' },
            {"ett", 'n' },
            {"ium", 'n' },
            {"ment", 'n' },
            {"tum", 'n' },
            {"eau", 'n' },
        }.OrderByDescending(a => a.Key.Length).ToDictionary();

        public char? GetGender(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            name = name.TryAfterLast(' ') ?? name;

            foreach (var kvp in terminations)
            {
                if (name.EndsWith(kvp.Key))
                    return kvp.Value;
            }

            return null;
        }

        ReadOnlyCollection<PronomInfo> pronoms = new ReadOnlyCollection<PronomInfo>(new[]
        {
            new PronomInfo('m', "der", "die"),
            new PronomInfo('f', "die", "die"),
            new PronomInfo('n', "das", "die"),
        });

        public ReadOnlyCollection<PronomInfo> Pronoms
        {
            get { return pronoms; }
        }
    }



}
