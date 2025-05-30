
namespace Signum.Utilities.NaturalLanguage;

public class EnglishPluralizer : IPluralizer
{
    //http://www.csse.monash.edu.au/~damian/papers/HTML/Plurals.html
    Dictionary<string, string> exceptions = new Dictionary<string, string>
    {
        {"ch", "ches"}, // church -> churches
        {"eau", "eaus"},  //chateau -> chateaus
        {"en", "ens"}, //foramen -> foramens
        {"ex", "exes"}, //index -> indexes
        {"f", "ves"}, //wolf -> wolves
        {"fe", "ves"}, //wolf -> wolves
        {"ieu", "ieus"}, //milieu-> mileus
        {"is", "is"}, //basis -> basis
        {"ix", "ixes"}, //matrix -> matrixes
        {"nx", "nxes"}, //phalanx -> phalanxes
        {"s", "s"}, //series -> series
        {"sh", "shes"}, //wish -> wishes
        {"us",  "us"},// genus -> us
        {"x",  "xes"},// box -> boxes
        {"ey", "eys" }, // key -> keys
        {"ay", "ays" }, // play -> plays
        {"oy", "oys" }, // boy -> boys
        {"uy", "uys" }, // guy -> guys
        {"y", "ies"}, //ferry -> ferries
        {"ss", "sses" } // class -> classes
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

    public string MakeSingular(string pluralName)
    {
        if (string.IsNullOrEmpty(pluralName))
            return pluralName;

        int index = pluralName.LastIndexOf(' ');

        if (index != -1)
            return pluralName.Substring(0, index + 1) + MakeSingular(pluralName.Substring(index + 1));

        var result = exceptions.FirstOrDefault(r => r.Value != "s" && pluralName.EndsWith(r.Value));
        if (result.Key != null)
            return pluralName.Substring(0, pluralName.Length - result.Value.Length) + result.Key;

        if (pluralName.EndsWith("s") || pluralName.EndsWith("S"))
            return pluralName.Substring(0, pluralName.Length - 1);

        return pluralName;
    }
}

