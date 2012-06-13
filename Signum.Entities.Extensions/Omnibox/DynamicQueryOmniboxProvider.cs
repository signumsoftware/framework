using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;

namespace Signum.Entities.Omnibox
{
    public class DynamicQueryOmniboxProvider :OmniboxProvider
    {
        public List<object> Queries;

        Regex regex = new Regex("I(.I)*=[NSID]");

        public override void AddResults(List<OmniboxResult> results, string rawQuery, List<OmniboxToken> tokens)
        {
            if(tokens.Count >= 1 && tokens[0].Type == OmniboxTokenType.Identifier)
            {
                string ident = tokens[0].Value;

                bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);

                foreach (var qn in Queries)
                {
                    int? priority = OmniboxUtils.GetPriority(qn.ToString(), QueryUtils.GetNiceName(qn), ident, isPascalCase);

                    var tokenPattern = new string(tokens.Select(t => Char(t.Type)).ToArray());



                    if (priority != null)
                    {
                          

                        
                    }
                }


                if (tokens[1].Type == OmniboxTokenType.Number)
                {
                    int id;
                    
                    if(!int.TryParse(tokens[1].Value, out id))
                        return;

                    string ident = tokens[0].Value;

                    bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);

                    foreach (var type in Queries)
                    {
                        int? priority = GetPriority(type.Name, type.NiceName(), ident, isPascalCase);

                        if (priority.HasValue)
                            results.Add(new EntityIdOmniboxResult { Priority = priority.Value, Id = id, Type = type });
                    }
                }
            }
        }

        private char Char(OmniboxTokenType omniboxTokenType)
        {
            switch (omniboxTokenType)
            {
                case OmniboxTokenType.Identifier: return 'I';
                case OmniboxTokenType.Dot: return '.';
                case OmniboxTokenType.Comparer: return '=';
                case OmniboxTokenType.Number: return 'N';
                case OmniboxTokenType.String: return 'S';
                case OmniboxTokenType.Date: return 'D';
                default: return '?';
            }
        }
    }

    public class DynamicQueryOmniboxResult : OmniboxResult
    {
        public object QueryName { get; set; }

    }

    public class FilterQueryResult
    {
        public int Priority;
        public QueryToken QueryToken; 



    }

    //User 2
    //ped cus.per.add.cit=="London" fj>'2012'

    //FVL N="hola"
    //
}
