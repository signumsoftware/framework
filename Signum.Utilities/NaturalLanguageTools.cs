using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace Signum.Utilities
{
    public static class NaturalLanguageTools
    {
        public static Dictionary<string, IPluralizer> Pluralizers = new Dictionary<string, IPluralizer>
        {
            {"es", new SpanishPluralizer()},
            {"en", new EnglishPluralizer()},
        };

        public static Dictionary<string, IGenderDetector> GenderDetectors = new Dictionary<string, IGenderDetector>
        {
            {"es", new SpanishGenderDetector()},
        };

        public static char? GetGender(string name, CultureInfo culture = null)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            IGenderDetector detector = GenderDetectors.TryGetC(culture.TwoLetterISOLanguageName);

            if (detector == null)
                return null;

            return detector.GetGender(name);
        }

        
        public static bool HasGenders(CultureInfo cultureInfo)
        {
            IGenderDetector detector = GenderDetectors.TryGetC(cultureInfo.TwoLetterISOLanguageName);

            if (detector == null)
                return false;

            return !detector.Pronoms.IsNullOrEmpty();
        }

        public static string GetPronom(char gender, bool plural, CultureInfo culture = null)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            IGenderDetector detector = GenderDetectors.TryGetC(culture.TwoLetterISOLanguageName);
            if (detector == null)
                return null;

            var pro = detector.Pronoms.FirstOrDefault(a => a.Gender == gender);

            if (pro == null)
                return null;

            return plural ? pro.Plural : pro.Singular;
        }

        public static string Pluralize(string singularName, CultureInfo culture = null)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            IPluralizer pluralizer = Pluralizers.TryGetC(culture.TwoLetterISOLanguageName);
            if (pluralizer == null)
                return singularName;

            return pluralizer.MakePlural(singularName);
        }


        public static string NiceName(this string memberName)
        {
            return memberName.Contains('_') ?
              memberName.Replace('_', ' ') :
              memberName.SpacePascal();
        }

        public static string SpacePascal(this string pascalStr, CultureInfo culture = null)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            return SpacePascal(pascalStr, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en");
        }

        public static string SpacePascal(this string pascalStr, bool preserveUppercase)
        {
            if (string.IsNullOrEmpty(pascalStr))
                return pascalStr;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < pascalStr.Length; i++)
            {
                switch (Kind(pascalStr, i))
                {
                    case CharKind.Lowecase:
                        sb.Append(pascalStr[i]);
                        break;

                    case CharKind.StartOfWord:
                        sb.Append(" ");
                        sb.Append(preserveUppercase ? pascalStr[i] : char.ToLower(pascalStr[i]));
                        break;

                    case CharKind.StartOfSentence:
                        sb.Append(pascalStr[i]);
                        break;

                    case CharKind.Abbreviation:
                        sb.Append(pascalStr[i]);
                        break;

                    case CharKind.StartOfAbreviation:
                        sb.Append(" ");
                        sb.Append(pascalStr[i]);
                        break;
                    default:
                        break;
                }
            }

            return sb.ToString();
        }

        static CharKind Kind(string pascalStr, int i)
        {
            if (i == 0)
                return CharKind.StartOfSentence;

            if (!char.IsUpper(pascalStr[i]))
                return CharKind.Lowecase;

            if (i + 1 == pascalStr.Length)
            {
                if (char.IsUpper(pascalStr[i - 1]))
                    return CharKind.Abbreviation;

                return CharKind.StartOfWord;
            }

            if (!char.IsUpper(pascalStr[i + 1]))
                return CharKind.StartOfWord;  // Xb

            if (!char.IsUpper(pascalStr[i - 1]))
            {
                if (i + 2 == pascalStr.Length)
                    return CharKind.StartOfAbreviation; //aXB|

                if (!char.IsUpper(pascalStr[i + 2]))
                    return CharKind.StartOfWord; //aXBc

                return CharKind.StartOfAbreviation; //aXBC
            }

            return CharKind.Abbreviation; //AXB
        }

        public enum CharKind
        {
            Lowecase,
            StartOfWord,
            StartOfSentence,
            StartOfAbreviation,
            Abbreviation,
        }


        /// <param name="genderAwareText">Se ha[n] encontrado [1m:un|1f:una|m:unos|f:unas] {0} eliminad[1m:o|1f:a|m:os|f:as]</param>
        /// <param name="gender">Masculine, Femenine, Neutrum, Inanimate, Animate</param>
        public static string ForGenderAndNumber(this string genderAwareText, char? gender = null, int? number = null)
        {
            if (gender == null && number == null)
                return genderAwareText;

            if (number == null)
                return GetPart(genderAwareText, gender + ":");

            if (gender == null)
            {
                if (number.Value == 1)
                    return GetPart(genderAwareText, "1:");

                return GetPart(genderAwareText, number.Value + ":", "");
            }

            if (number.Value == 1)
                return GetPart(genderAwareText, "1" + gender.Value + ":", "1:");

            return GetPart(genderAwareText, gender.Value + number.Value + ":", gender.Value + ":", number.Value + ":", "");
        }

        static string GetPart(string textToReplace, params string[] prefixes)
        {
            return Regex.Replace(textToReplace,
              @"\[(?<part>[^\|\]]+)(\|(?<part>[^\|\]]+))*\]", m =>
              {
                  var captures = m.Groups["part"].Captures.OfType<Capture>();

                  foreach (var pr in prefixes)
                  {
                      Capture capture = captures.FirstOrDefault(c => c.Value.StartsWith(pr));
                      if (capture != null)
                          return capture.Value.RemoveStart(pr.Length);
                  }

                  return "";
              });
        }


    }

    public interface IPluralizer
    {
        string MakePlural(string singularName);
    }

    public class EnglishPluralizer : IPluralizer
    {
        //http://www.csse.monash.edu.au/~damian/papers/HTML/Plurals.html
        Dictionary<string, string> exceptions = new Dictionary<string, string>
        {
            {"an", "en"}, // woman -> women 
            {"ch", "ches"}, // church -> churches 
            {"eau", "eaus"},  //chateau -> chateaus
            {"en", "ens"}, //foramen -> foramens
            {"ex", "exes"}, //index -> indexes
            {"f", "ves"}, //wolf -> wolves 
            {"fe", "ves"}, //wolf -> wolves 
            {"ieu", "ieus milieu"}, //milieu-> mileus
            {"is", "is"}, //basis -> basis 
            {"ix", "ixes"}, //matrix -> matrixes
            {"nx", "nxes"}, //phalanx -> phalanxes 
            {"s", "s"}, //series -> series 
            {"sh", "shes"}, //wish -> wishes 
            {"us",  "us"},// genus -> us 
            {"x",  "xes"},// box -> boxes 
            {"y", "ies"}, //ferry -> ferries 
        };

        public string MakePlural(string singularName)
        {
            if (string.IsNullOrEmpty(singularName))
                return singularName;

            int index = singularName.LastIndexOf(' ');

            if (index != -1)
                return singularName.Substring(0, index + 1) + MakePlural(singularName.Substring(index + 1));

            var result = exceptions.FirstOrDefault(r => singularName.EndsWith(r.Key));
            if (result.Value != null)
                return singularName.Substring(0, singularName.Length - result.Key.Length) + result.Value;

            return singularName + "s";
        }
    }

    public class SpanishPluralizer : IPluralizer
    {
        //http://es.wikipedia.org/wiki/Formaci%C3%B3n_del_plural_en_espa%C3%B1ol
        Dictionary<string, string> exceptions = new Dictionary<string, string>
        {
            {"x", "x"}, // tórax -> tórax
            {"s", "s"}, // suponemos que el nombre ya está pluralizado
            {"z", "ces"},  //vez -> veces
            {"g", "gues"}, //zigzag -> zigzagues
            {"c", "ques"}, //frac -> fraques
            {"t", "ts"}, //mamut -> mamuts
            {"án", "anes"},
            {"én", "enes"},
            {"ín", "ines"},
            {"ón", "ones"},
            {"ún", "unes"},
        };

        char[] vowels = new[] { 'a', 'e', 'i', 'o', 'u', 'á', 'é', 'í', 'ó', 'ú' };

        public string MakePlural(string singularName)
        {
            if (string.IsNullOrEmpty(singularName))
                return singularName;

            int index = singularName.IndexOf(' ');

            if (index != -1)
                return MakePlural(singularName.Substring(0, index)) + singularName.Substring(index);

            var result = exceptions.FirstOrDefault(r => singularName.EndsWith(r.Key));
            if (result.Value != null)
                return singularName.Substring(0, singularName.Length - result.Key.Length) + result.Value;

            char lastChar = singularName[singularName.Length - 1];
            if (vowels.Contains(lastChar))
                return singularName + "s";
            else
                return singularName + "es";
        }
    }

    public interface IGenderDetector
    {
        char? GetGender(string name);

        ReadOnlyCollection<PronomInfo> Pronoms { get; }
    }

    public class PronomInfo
    {
        public char Gender { get; private set; }
        public string Singular { get; private set; }
        public string Plural { get; private set; }

        public PronomInfo(char gender, string singular, string plural)
        {
            this.Gender = gender;
            this.Singular = singular;
            this.Plural = plural;
        }
    }

    public class SpanishGenderDetector : IGenderDetector
    {
        //http://roble.pntic.mec.es/acid0002/index_archivos/Gramatica/genero_sustantivos.htm
        Dictionary<string, char> terminationIsFemenine = new Dictionary<string, char>()
        {
            {"umbre", 'f' },
           
            {"ión", 'f' },
            {"dad", 'f' },
            {"tad", 'f' },
            
            {"ie", 'f' },
            {"is", 'f' }, 

            {"pa", 'f'},
            //{"ta", Gender.Masculine}, Cuenta, Nota, Alerta... son femeninos
            {"ma", 'f'},

            {"a", 'f'},
            {"n", 'm'},
            {"o", 'm'},
            {"r", 'm'},
            {"s", 'm'},
            {"e", 'm'},
            {"l", 'm'},

            {"", 'm'},
        };


        public char? GetGender(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            int index = name.IndexOf(' ');

            if (index != -1)
                return GetGender(name.Substring(0, index));

            foreach (var kvp in terminationIsFemenine)
            {
                if (name.EndsWith(kvp.Key))
                    return kvp.Value;
            }

            return null;
        }

        ReadOnlyCollection<PronomInfo> pronoms = new ReadOnlyCollection<PronomInfo>(new []
        {
            new PronomInfo('m', "el", "los"),
            new PronomInfo('f', "la", "las")
        });

        public ReadOnlyCollection<PronomInfo> Pronoms
        {
            get { return pronoms; }
        }
    }
}
