using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Signum.Utilities.NaturalLanguage;

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

public class SpanishGenderDetector : IGenderDetector
{
    //http://roble.pntic.mec.es/acid0002/index_archivos/Gramatica/genero_sustantivos.htm
    Dictionary<string, char> terminations = new Dictionary<string, char>()
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

        //{"", 'm'},
    };


    public char? GetGender(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        name = name.TryBefore(' ') ?? name;

        foreach (var kvp in terminations)
        {
            if (name.EndsWith(kvp.Key))
                return kvp.Value;
        }

        return null;
    }

    ReadOnlyCollection<PronomInfo> pronoms = new ReadOnlyCollection<PronomInfo>(new[]
    {
        new PronomInfo('m', "el", "los"),
        new PronomInfo('f', "la", "las")
    });

    public ReadOnlyCollection<PronomInfo> Determiner
    {
        get { return pronoms; }
    }
}

public class SpanishNumberWriter : INumberWriter
{
    public static NumberWriterSettings EuroSettings = new NumberWriterSettings
    {
        Unit = "euro",
        UnitPlural = "euros",
        UnitGender = null,

        DecimalUnit = "céntimo",
        DecimalUnitPlural = "céntimos",
        DecimalUnitGender = null,

        NumberOfDecimals = 2,
        OmitDecimalZeros = true
    };


    public string ToNumber(decimal number, NumberWriterSettings settings)
    {
        bool? femenin =
            settings.UnitGender == 'f' ? (bool?)true :
            settings.UnitGender == 'm' ? (bool?)false : null;

 	        string? signo = null;
        string? integerPart = null;
        string? decimalPart = null;

        if (number < 0)
        {
            signo = "menos";
            number = -number;
        }

        //convertimos en text0
        long integer = (long)number;
        integerPart = ConvertNumber(integer, femenin, settings.Unit, settings.UnitPlural);

        decimal decimAux = (number - integer);
        for (int i = 0; i < settings.NumberOfDecimals; i++)
            decimAux *=10m;

        long decim = (long)decimAux;
        if (decim != decimAux)
            throw new ApplicationException(string.Format("number has more than {0} decimal values", settings.NumberOfDecimals));


        if (decim != 0 || !settings.OmitDecimalZeros)
        {
            decimalPart = ConvertNumber(decim, femenin, settings.DecimalUnit, settings.DecimalUnitPlural);
        }

        return " ".Combine(signo, " con ".Combine(integerPart, decimalPart));
    }
    private static string ConvertNumber(long num, bool? femenine, string singular, string plural)
    {
        string? result = null;
        long numAux = num;
        for (int i = 0; numAux > 0; i++, numAux /= 1000)
            result = " ".Combine(ConvertTrio((int)(numAux % 1000), i, femenine), " ", result);

        long numMod1M = num % 1000000;

        string separator = numMod1M == 0 && num != 0 ? " de " : " ";

        return separator.Combine(result ?? "cero", numMod1M == 1 ? singular : plural);
    }

    static string? ConvertTrio(int val, int group, bool? femenine)
    {
        string trio = val.ToString("000");

        int cent = CharUtil.ToInt(trio[0]);
        int dec = CharUtil.ToInt(trio[1]);
        int unit = CharUtil.ToInt(trio[2]);

        if (cent == 0 && dec == 0 && unit == 0 && group % 2 == 1)
            return null;

        string? groupName = UnitsGroup(group, unit != 1 || dec > 0 || cent > 0);

        string num = CentsDecsUnits(cent, dec, unit, group >= 2 ? null : femenine);

        return " ".Combine(val == 1 && groupName == "mil" ? null : num, groupName);
    }

    static string? UnitsGroup(int numGroup, bool plural)
    {
        //en función de la cantidad de elementos que haya en enteros tendremos los
        //billones, millones, unidades...
        switch (numGroup)
        {
            case 0: return null;
            case 1:
            case 3:
            case 5:
            case 7:
            case 9: return "mil";

            case 2: return plural ? "millones" : "millón";
            case 4: return plural ? "billones" : "billón";
            case 6: return plural ? "trillones" : "trillón";
            case 8: return plural ? "cuatrillones" : "cuatrillón";
            default: throw new InvalidOperationException("valores superiores a miles de cuatrillones no soportados");
        }
    }

    static string CentsDecsUnits(int centena, int decena, int unidad, bool? femenine)
    {
        return " ".Combine((centena == 1 && decena == 0 && unidad == 0) ? "cien" : Cents(centena, femenine ?? false), " ",
            DecsUnits(decena, unidad, femenine));
    }

    static string DecsUnits(int decena, int unit, bool? femenine)
    {
        switch (decena)
        {
            case 1:
                //se trata de once, doce...
                switch (unit)
                {
                    case 0: return "diez";
                    case 1: return "once";
                    case 2: return "doce";
                    case 3: return "trece";
                    case 4: return "catorce";
                    case 5: return "quince";
                    case 6: return "dieciséis";
                    case 7: return "diecisiete";
                    case 8: return "dieciocho";
                    case 9: return "diecinueve";
                    default: throw new InvalidOperationException();
                }
            case 2:
                switch (unit)
                {
                    case 0: return "veinte";
                    case 1: return
                        femenine == true ? "veintiuna" :
                        femenine == false ? "veitiuno" : "veintiún";
                    default: return "veinti" + Units(unit, femenine);
                }
            default:
                return " y ".Combine(Decs(decena), Units(unit, femenine));
        }
    }

    static string? Units(int num, bool? femenine)
    {
        switch (num)
        {
            case 0: return null;
            case 1: return
                femenine == true ? "una" :
                femenine == false ? "uno" : "un";
            case 2: return "dos";
            case 3: return "tres";
            case 4: return "cuatro";
            case 5: return "cinco";
            case 6: return "seis";
            case 7: return "siete";
            case 8: return "ocho";
            case 9: return "nueve";
            default: throw new InvalidOperationException();
        }
    }

    static string? Decs(int num)
    {
        switch (num)
        {
            case 0: return null;
            case 1: return "diez";
            case 2: return "veinte";
            case 3: return "treinta";
            case 4: return "cuarenta";
            case 5: return "cincuenta";
            case 6: return "sesenta";
            case 7: return "setenta";
            case 8: return "ochenta";
            case 9: return "noventa";
            default: throw new InvalidOperationException();
        }
    }

    static string? Cents(int num, bool femenine)
    {
        switch (num)
        {
            case 0: return null;
            case 1: return "ciento";
            case 2: return femenine ? "doscientas" : "doscientos";
            case 3: return femenine ? "trescientas" : "trescientos";
            case 4: return femenine ? "cuentrocientas" : "cuatrocientos";
            case 5: return femenine ? "quinientas" : "quinientos";
            case 6: return femenine ? "seiscientas" : "seiscientos";
            case 7: return femenine ? "setecientas" : "setecientos";
            case 8: return femenine ? "ochocientas" : "ochocientos";
            case 9: return femenine ? "novecientas" : "novecientos";
            default: throw new InvalidOperationException();
        }
    }
}

public static class CharUtil
{
    public static int ToInt(char val)
    {
        return val - '0';
    }

    public static char ToChar(int val)
    {
        return (char)('0' + val);
    }

    public static Regex IsNumeric = new Regex(@"^\d*$");
}
