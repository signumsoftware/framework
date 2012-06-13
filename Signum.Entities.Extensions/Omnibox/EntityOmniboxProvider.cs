using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Omnibox
{
    public class EntityOmniboxProvider :OmniboxProvider
    {
        public List<Type> AvailableTypes;

        public override void AddResults(List<OmniboxResult> results, string rawQuery, List<OmniboxToken> tokens)
        {
            if(tokens.Count == 2 && tokens[0].Type == OmniboxTokenType.Identifier)
            {
                if (tokens[1].Type == OmniboxTokenType.Number)
                {
                    int id;
                    
                    if(!int.TryParse(tokens[1].Value, out id))
                        return;

                    string ident = tokens[0].Value;

                    bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);

                    foreach (var type in AvailableTypes)
                    {
                        int? priority = OmniboxUtils.GetPriority(type.Name, type.NiceName(), ident, isPascalCase);

                        if (priority.HasValue)
                            results.Add(new EntityIdOmniboxResult { Priority = priority.Value, Id = id, Type = type });
                    }
                }
                else if (tokens[1].Type == OmniboxTokenType.String)
                {
                    string toStr = tokens[1].Value;

                    string ident = tokens[0].Value;

                    bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);

                    foreach (var type in AvailableTypes)
                    {
                        int? priority = OmniboxUtils.GetPriority(type.Name, type.NiceName(), ident, isPascalCase);

                        if (priority.HasValue)
                            results.Add(new EntityToStringOmniboxResult { Priority = priority.Value, ToStr = toStr, Type = type });
                    }
                }
            }
        }
    }

    public class EntityIdOmniboxResult : OmniboxResult
    {
        public Type Type { get; set; }
        public int Id { get; set; }
    }

    public class EntityToStringOmniboxResult : OmniboxResult
    {
        public Type Type { get; set; }
        public string ToStr { get; set; }
    }
}
