using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities.NaturalLanguage
{
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
    }

}
