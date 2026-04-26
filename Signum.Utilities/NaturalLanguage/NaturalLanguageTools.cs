using Signum.Utilities.NaturalLanguage;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.Utilities;

public static class NaturalLanguageTools
{
    public static Dictionary<string, IPluralizer> Pluralizers = new Dictionary<string, IPluralizer>
    {
        {"es", new SpanishPluralizer()},
        {"en", new EnglishPluralizer()},
        {"de", new GermanPluralizer()},
    };

    public static Dictionary<string, IGenderDetector> GenderDetectors = new Dictionary<string, IGenderDetector>
    {
        {"es", new SpanishGenderDetector()},
        {"de", new GermanGenderDetector()},
    };

    static Dictionary<string, string> SimpleDeterminers = new Dictionary<string, string>()
    {
        {"en", "the"}
    };

    public static Dictionary<string, INumberWriter> NumberWriters = new Dictionary<string, INumberWriter>
    {
        {"es", new SpanishNumberWriter()},
        {"de", new GermanNumberWriter()},
    };

    public static Dictionary<string, IDiacriticsRemover> DiacriticsRemover = new Dictionary<string, IDiacriticsRemover>
    {
        {"de", new GermanDiacriticsRemover()},
    };



    public static char? GetGender(string name, CultureInfo? culture = null)
    {
        var defCulture = culture ?? CultureInfo.CurrentUICulture;

        var detector = GenderDetectors.TryGetC(defCulture.TwoLetterISOLanguageName);

        if (detector == null)
            return null;

        return detector.GetGender(name);
    }

    public static bool HasGenders(CultureInfo cultureInfo)
    {
        var detector = GenderDetectors.TryGetC(cultureInfo.TwoLetterISOLanguageName);

        if (detector == null)
            return false;

        return !detector.Determiner.IsNullOrEmpty();
    }


    public static string? GetDeterminer(char? gender, bool plural, CultureInfo? culture = null)
    {
        if (culture == null)
            culture = CultureInfo.CurrentUICulture;

        var detector = GenderDetectors.TryGetC(culture.TwoLetterISOLanguageName);
        if (detector == null)
            return SimpleDeterminers.TryGetC(culture.TwoLetterISOLanguageName);

        var pro = detector.Determiner.FirstOrDefault(a => a.Gender == gender);

        if (pro == null)
            return null;

        return plural ? pro.Plural : pro.Singular;
    }

    public static bool TryGetGenderFromDeterminer(string? determiner, bool plural, CultureInfo culture, out char? gender)
    {
        gender = null;
        if (culture == null)
            culture = CultureInfo.CurrentUICulture;

        if (determiner == null)
            return false;

        var detector = GenderDetectors.TryGetC(culture.TwoLetterISOLanguageName);
        if (detector == null)
            return SimpleDeterminers.TryGetC(culture.TwoLetterISOLanguageName) == determiner;

        var pro = detector.Determiner.FirstOrDefault(a => (plural ? a.Plural : a.Singular) == determiner);

        if (pro != null)
        {
            gender = pro.Gender;
            return true;
        }

        return false;
    }

    public static string Pluralize(string singularName, CultureInfo? culture = null)
    {
        if (culture == null)
            culture = CultureInfo.CurrentUICulture;

        var pluralizer = Pluralizers.TryGetC(culture.TwoLetterISOLanguageName);
        if (pluralizer == null)
            return singularName;

        return pluralizer.MakePlural(singularName);
    }


    public static string SpacePascalOrUnderscores(this string memberName)
    {
        return memberName.Contains('_') ?
          memberName.Replace('_', ' ') :
          memberName.SpacePascal();
    }

    public static string SpacePascal(this string pascalStr, CultureInfo? culture = null)
    {
        var defCulture = culture ?? CultureInfo.CurrentUICulture;

        return SpacePascal(pascalStr, false);
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

                case CharKind.StartOfAbbreviation:
                    sb.Append(" ");
                    sb.Append(pascalStr[i]);
                    break;
                default:
                    break;
            }
        }

        return sb.ToString();
    }

    public static string PascalToSnake(this string pascalStr)
    {
        if (string.IsNullOrEmpty(pascalStr))
            return pascalStr;

        string result = Regex.Replace(pascalStr, @"([a-z0-9])([A-Z])", "$1_$2");
        result = Regex.Replace(result, @"([A-Z]+)([A-Z][a-z])", "$1_$2");

        return result.ToLower();
    }

    static CharKind Kind(string pascalStr, int i)
    {
        if (i == 0)
            return CharKind.StartOfSentence;

        if (!char.IsLetter(pascalStr[i]))
        {
            if (char.IsLetter(pascalStr[i - 1]))
                return CharKind.StartOfWord;

            return CharKind.Lowecase;
        }

        if (!char.IsUpper(pascalStr[i]))
            return CharKind.Lowecase;

        if (i + 1 == pascalStr.Length || !char.IsLetter(pascalStr[i + 1]))
        {
            if (char.IsUpper(pascalStr[i - 1])) //AX|
                return CharKind.Abbreviation; 

            return CharKind.StartOfAbbreviation;  //aX|
        }

        if (char.IsLower(pascalStr[i + 1]))
            return CharKind.StartOfWord;  // Xb

        if (!char.IsUpper(pascalStr[i - 1]))
        {
            if (i + 2 == pascalStr.Length)
                return CharKind.StartOfAbbreviation; //aXB|

            if (!char.IsUpper(pascalStr[i + 2]))
                return CharKind.StartOfWord; //aXBc

            return CharKind.StartOfAbbreviation; //aXBC
        }

        return CharKind.Abbreviation; //AXB
    }

    public enum CharKind
    {
        Lowecase,
        StartOfWord,
        StartOfSentence,
        StartOfAbbreviation,
        Abbreviation,
    }

    public static string ToPascal(this string str)
    {
        return str.ToPascal(firstUpper: true, keepUppercase: false);
    }

    public static string ToPascal(this string str, bool firstUpper, bool keepUppercase)
    {
        str = str.RemoveDiacritics();

        StringBuilder sb = new StringBuilder(str.Length);

        bool upper = firstUpper;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (!char.IsLetter(c) && !char.IsNumber(c))
                upper = true;
            else
            {
                sb.Append(upper ? char.ToUpper(c) :
                    keepUppercase ? c : char.ToLower(c));

                if (char.IsLetter(c))
                    upper = false;
            }
        }

        return sb.ToString();
    }

    /// <param name="genderAwareText">Something like Line[s] or [1m:Man|m:Men|1f:Woman|f:Women]</param>
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

            return GetPart(genderAwareText, number.Value + ":", ":", "");
        }

        if (number.Value == 1)
            return GetPart(genderAwareText, "1" + gender.Value + ":", "1:");

        return GetPart(genderAwareText, gender.Value + number.Value + ":", gender.Value + ":", number.Value + ":", ":");
    }

    static string GetPart(string textToReplace, params string[] prefixes)
    {
        return Regex.Replace(textToReplace,
          @"\[(?<part>[^\|\]]+)(\|(?<part>[^\|\]]+))*\]", m =>
          {
              var captures = m.Groups["part"].Captures.OfType<Capture>();

              foreach (var pr in prefixes)
              {
                  Capture? capture = captures.FirstOrDefault(c => c.Value.StartsWith(pr));
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

public interface IGenderDetector
{
    char? GetGender(string name);

    ReadOnlyCollection<PronomInfo> Determiner { get; }
}

public class PronomInfo
{
    public char Gender { get; private set; }
    public string Singular { get; private set; }
    public string Plural { get; private set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public PronomInfo() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public PronomInfo(char gender, string singular, string plural)
    {
        this.Gender = gender;
        this.Singular = singular;
        this.Plural = plural;
    }
}

public interface INumberWriter
{
    string ToNumber(decimal number, NumberWriterSettings settings);
}

public interface IDiacriticsRemover
{
    string RemoveDiacritics(string str);
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class NumberWriterSettings
{
    public string Unit;
    public string UnitPlural;
    public char? UnitGender;

    public string DecimalUnit;
    public string DecimalUnitPlural;
    public char? DecimalUnitGender;

    public int NumberOfDecimals;
    public bool OmitDecimalZeros;
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

public static class NumberFormatter
{
    private static bool TryExtractCompactFormat(string? format, out string innerFormat)
    {
        if (!string.IsNullOrEmpty(format) && format.StartsWith("K"))
        {
            innerFormat = format.Length > 1 ? format.Substring(1) : "0.#";
            return true;
        }
        innerFormat = default!;
        return false;
    }

    public static string ToStringWithCompact(this decimal number, string? format, CultureInfo? culture = null)
        => TryExtractCompactFormat(format, out var f) ? FormatCompact(number, f, culture) : number.ToString(format, culture);

    public static string ToStringWithCompact(this double number, string? format, CultureInfo? culture = null)
        => TryExtractCompactFormat(format, out var f) ? FormatCompact((decimal)number, f, culture) : number.ToString(format, culture);

    public static string ToStringWithCompact(this float number, string? format, CultureInfo? culture = null)
        => TryExtractCompactFormat(format, out var f) ? FormatCompact((decimal)number, f, culture) : number.ToString(format, culture);

    public static string ToStringWithCompact(this long number, string? format, CultureInfo? culture = null)
        => TryExtractCompactFormat(format, out var f) ? FormatCompact(number, f, culture) : number.ToString(format, culture);

    public static string ToStringWithCompact(this int number, string? format, CultureInfo? culture = null)
       => TryExtractCompactFormat(format, out var f) ? FormatCompact(number, f, culture) : number.ToString(format, culture);

    public static string ToStringWithCompact(this IFormattable number, string? format, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (number == null) throw new ArgumentNullException(nameof(number));
        if (TryExtractCompactFormat(format, out var f))
        {
            return number switch
            {
                decimal d => FormatCompact(d, f, culture),
                double d => FormatCompact((decimal)d, f, culture),
                float f2 => FormatCompact((decimal)f2, f, culture),
                long l => FormatCompact(l, f, culture),
                int l => FormatCompact(l, f, culture),
                _ => number.ToString(format, culture)
            };
        }
        return number.ToString(format, culture);
    }


    public static string FormatCompact(decimal number, string format = "0.#", CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (Math.Abs(number) < 1000)
            return number.ToString(format, culture);

        int suffixIndex = 0;
        decimal reducedNumber = Math.Abs(number);

        // Loop to reduce the number by thousands until it fits or enum max
        while (reducedNumber >= 1000 && suffixIndex < Enum.GetValues(typeof(NumberUnitsMessage)).Length - 1)
        {
            reducedNumber /= 1000;
            suffixIndex++;
        }

        string formattedNumber = reducedNumber.ToString(format, culture);

        if (number < 0)
            formattedNumber = "-" + formattedNumber;

        if (suffixIndex == 0)
            return formattedNumber;

        // Cast suffixIndex to NumberUnits enum
        NumberUnitsMessage unit = (NumberUnitsMessage)suffixIndex;

        return formattedNumber + unit.NiceToString();
    }
}

public enum NumberUnitsMessage
{
    [Description("K")]
    Thousand = 1,

    [Description("M")]
    Million = 2,

    [Description("B")]
    Billion = 3,

    [Description("T")]
    Trillion = 4,

    [Description("Q")]
    Quadrillion = 5,

    [Description("Qi")]
    Quintillion = 6,

    [Description("Sx")]
    Sextillion = 7,

    [Description("Sp")]
    Septillion = 8,

    [Description("Oc")]
    Octillion = 9,

    [Description("No")]
    Nonillion = 10
}
