using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Omnibox
{
    public abstract class EntityOmniboxProvider : OmniboxProvider
    {
        Dictionary<string, Type> types; 

        public EntityOmniboxProvider(IEnumerable<Type> schemaTypes)
        {
            types = schemaTypes.Where(t => !t.IsEnumProxy()).ToDictionary(t => Lite.UniqueTypeName(t));
        }

        protected abstract bool Allowed(Type type);

        protected abstract List<Lite> AutoComplete(Type type, string subString);

        protected abstract Lite RetrieveLite(Type type, int id);

        public override void AddResults(List<OmniboxResult> results, string rawQuery, List<OmniboxToken> tokens)
        {
            if (tokens.Count == 0 || tokens.Count > 2 || tokens[0].Type != OmniboxTokenType.Identifier)
                return;

            if (tokens.Count == 2 && (tokens[1].Type != OmniboxTokenType.Number && tokens[1].Type != OmniboxTokenType.String))
                return;

            string ident = tokens[0].Value;

            bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);

            var matches = OmniboxUtils.Matches(types, t => t.NiceName(), ident, isPascalCase).Where(a => Allowed((Type)a.Value));

            if (tokens.Count == 1)
            {
                foreach (var m in matches)
                {
                    results.Add(new EntityOmniboxResult
                    {
                        Type = (Type)m.Value,
                        TypeMatch = m,
                        Distance = m.Distance,
                    });
                }
            }
            else if (tokens[1].Type == OmniboxTokenType.Number)
            {
                int id;
                if (!int.TryParse(tokens[1].Value, out id))
                    return;

                foreach (var m in matches)
                {
                    Lite lite = RetrieveLite((Type)m.Value, id);

                    results.Add(new EntityOmniboxResult
                    {
                        Type = (Type)m.Value,
                        TypeMatch = m,
                        Id = id,
                        Lite = lite,
                        Distance = m.Distance,
                    });
                }
            }
            else if (tokens[1].Type == OmniboxTokenType.String)
            {
                string pattern = OmniboxUtils.CleanCommas(tokens[1].Value);

                foreach (var m in matches)
                {
                    var autoComplete = AutoComplete((Type)m.Value, pattern);

                    if (autoComplete.Any())
                    {
                        foreach (Lite lite in autoComplete)
                        {
                            OmniboxMatch distance = OmniboxUtils.Contains(lite, lite.ToString(), pattern);

                            results.Add(new EntityOmniboxResult
                            {
                                Type = (Type)m.Value,
                                TypeMatch = m,
                                ToStr = pattern,
                                Lite = lite,
                                Distance = m.Distance + distance.Distance,
                                ToStrMatch = distance,
                            });
                        }
                    }
                    else
                    {
                        results.Add(new EntityOmniboxResult
                        {
                            Type = (Type)m.Value,
                            TypeMatch = m,
                            Distance = m.Distance + 100,
                            ToStr = pattern,
                        });
                    }
                }

            }
        }

    }

    public class EntityOmniboxResult : OmniboxResult
    {
        public Type Type { get; set; }
        public OmniboxMatch TypeMatch { get; set; }

        public int? Id { get; set; }

        public string ToStr { get; set; }
        public OmniboxMatch ToStrMatch { get; set; }

        public Lite Lite { get; set; }

        public override string ToString()
        {
            if (Id.HasValue)
                return "{0} {1}: {2}".Formato(Type.NiceName(), Id, Lite.TryToString() ?? Resources.NotFound);

            if (ToStr != null)
                return "{0} \"{1}\": {2}".Formato(Type.NiceName(), ToStr, Lite.TryToString() ?? Resources.NotFound);

            return "{0}...".Formato(Type.NiceName());
        }
    }
}
