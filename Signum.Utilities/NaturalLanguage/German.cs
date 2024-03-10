using System.Collections.ObjectModel;
using System.Globalization;

namespace Signum.Utilities.NaturalLanguage;

public class GermanPluralizer : IPluralizer
{
    //http://www.alemansencillo.com/el-plural-en-aleman#TOC-Reglas-generales-aplicables-a-todos
    readonly Dictionary<string, string> terminationsFemenine = new Dictionary<string, string>
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
    readonly Dictionary<string, string> terminationsMasculine = new Dictionary<string, string>
    {
        {"ant", "anten"},
        {"ent", "enten"},
        {"ist", "isten"},
        {"at", "aten"},
        {"us", "usse"},
        {"e", "en"},
        {"", "e"},
    };
    readonly Dictionary<string, string> terminationsNeutro = new Dictionary<string, string>
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

        string? last = singularName.TryAfterLast(' ');
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
    readonly Dictionary<string, char> terminations = new Dictionary<string, char>
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
        {"typ", 'm' },
        {"code", 'm' },

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
        {"e", 'f' },
        {"art", 'f' },

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

    public ReadOnlyCollection<PronomInfo> Determiner { get; } = new ReadOnlyCollection<PronomInfo>(new[]
    {
        new PronomInfo('m', "der", "die"),
        new PronomInfo('f', "die", "die"),
        new PronomInfo('n', "das", "die"),
    });
}

public class GermanNumberWriter : INumberWriter
{
    public static NumberWriterSettings EuroSettings = new NumberWriterSettings
    {
        Unit = "Euro",
        UnitPlural = "Euro",
        UnitGender = null,

        DecimalUnit = "Cent",
        DecimalUnitPlural = "Cent",
        DecimalUnitGender = null,

        NumberOfDecimals = 2,
        OmitDecimalZeros = true,
    };

    CultureInfo de = CultureInfo.GetCultureInfo("de");

    public string ToNumber(decimal number, NumberWriterSettings settings)
    {
        /// <summary>
        /// Converts a decimal to its written text. e.g. 12 --> "zwölf"
        /// </summary>
        /// <param name="number">Number to be written as text. Can only be greater equal to 0 and less then one billion </param>
        /// <returns>String number</returns>
        /// 

        string[] numberString = number.ToString(de).Split(',');

        string euro = numberString[0];            

        string output = "";

        if (number < 0) throw new Exception(" Can not convert values under 0 to a String");
        if (1000000000 < number) throw new Exception(" Can not convert values over a billion to a String");
        if (number < 1) output += "Null";


        else
        {
            if (euro.Length <= 3 && euro.Length > 0) // 0 -> 999
            {
                output = GetUpToThousand(Convert.ToInt32(euro));
            }

            else if (euro.Length <= 6 && euro.Length > 3) //1000 -> 999.999
            {
                string hunderter = euro.Substring(euro.Length - 3);
                string tausender = euro.Substring(0, euro.Length - 3);
                output
                = GetUpToThousand(Convert.ToInt32(tausender))
                + "tausend"
                + GetUpToThousand(Convert.ToInt32(hunderter));
            }
            else if (euro.Length <= 9 && euro.Length > 6)
            {
                string hunderter = euro.Substring(euro.Length - 3);
                string tausender = euro.Substring(euro.Length - 6, 3);
                string million = euro.Substring(0, euro.Length - 6);

                //Ausnahme falls eine million
                if (1000000 <= number && number < 2000000)
                {
                    output = "eine Million";
                    if (Convert.ToInt32(tausender) != 0)
                    {
                        output += GetUpToThousand(Convert.ToInt32(tausender)) + "tausend";
                    }
                    output += GetUpToThousand(Convert.ToInt32(hunderter));
                }
                else
                {
                    output = GetUpToThousand(Convert.ToInt32(million))
                    + " Millionen ";

                    if (Convert.ToInt32(tausender) != 0)
                    {
                        output += GetUpToThousand(Convert.ToInt32(tausender)) + "tausend";
                    }
                    output += GetUpToThousand(Convert.ToInt32(hunderter));
                }
            }
        }

        //cent operation
        if(settings.NumberOfDecimals == 2)
        {
            if (numberString.Length < 2)
            {
                output += " 00/100";
            }
            else
            {
                output += " " + numberString[1] + "/100";
            }
        }
        return output;
    }

    /// <summary>
    /// returns a string value for a number with up to 3 digits e.g. einhundertzweiundzwanzig or siebenundsiebzig
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private static string GetUpToThousand(int number)
    {
        if (1000 <= number) throw new Exception("Can only convert numbers less than thousand");
        if (number < 100)
        {
            return GetUpToHundred(number);
        }
        else
        {
            int firstDigit = Convert.ToInt32(number.ToString().Substring(0, 1));
            int lastDigits = Convert.ToInt32(number.ToString().Substring(1, 2));
            return EinerArray[firstDigit] + "hundert" + GetUpToHundred(lastDigits);
        }
    }

    private static string GetUpToHundred(int number)
    {
        string output = "";
        if (number <= 12)
        {
            return EinerArray[number];
        }
        switch (number)
        {
            case 16: return "sechzehn";
            case 17: return "siebzehn";
        }
        string tempString = number.ToString();

        if (number < 20)
        {
            int einerWert = Convert.ToInt32(number.ToString().Substring(1, 1));
            return EinerArray[einerWert] + "zehn";
        }
        else
        {
            int einerWert = Convert.ToInt32(number.ToString().Substring(1, 1));
            int zehnerWert = Convert.ToInt32(number.ToString().Substring(0, 1));
            if (einerWert > 0)
            {
                output += EinerArray[einerWert] + "und";
            }
            output +=  ZehnerArray[zehnerWert];
            return output;
        }
    }

    private static string[] EinerArray = { "", "ein", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht", "neun", "zehn", "elf", "zwölf" };
    private static string[] ZehnerArray = { "", "zehn", "zwanzig", "dreißig", "vierzig", "fünfzig", "sechzig", "siebzig", "achtzig", "neunzig" };


}

public class GermanDiacriticsRemover : IDiacriticsRemover
{
    public string RemoveDiacritics(string str)
    {
        return str
            .Replace("ü", "ue")
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("Ü", "Ue")
            .Replace("Ä", "Ae")
            .Replace("Ö", "Oe")
            .Replace("ß", "ss")
            .Replace("ẞ", "Ss");
        

    }
}
