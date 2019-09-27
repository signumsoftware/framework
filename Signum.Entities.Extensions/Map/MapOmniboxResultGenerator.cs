using Newtonsoft.Json;
using Signum.Entities.Basics;
using Signum.Entities.Omnibox;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Signum.Entities.Map
{

    public class MapOmniboxResultGenerator : OmniboxResultGenerator<MapOmniboxResult>
    {
        public Func<string> NiceName = () => MapMessage.Map.NiceToString();

        public Func<Type, bool> HasOperations;

        public MapOmniboxResultGenerator(Func<Type, bool> hasOperations)
        {
            this.HasOperations = hasOperations;
        }

        Regex regex = new Regex(@"^II?$", RegexOptions.ExplicitCapture);
        public override IEnumerable<MapOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (!OmniboxParser.Manager.AllowedPermission(MapPermission.ViewMap))
                yield break;

            Match m = regex.Match(tokenPattern);

            if (!m.Success)
                yield break;

            string key = tokens[0].Value;

            var keyMatch = OmniboxUtils.Contains(NiceName(), NiceName(), key);

            if (keyMatch == null)
                yield break;

            if (tokens.Count == 1)
                yield return new MapOmniboxResult { Distance = keyMatch.Distance, KeywordMatch = keyMatch };

            else
            {
                string pattern = tokens[1].Value;

                bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

                var types = OmniboxParser.Manager.Types().Where(a => HasOperations(a.Value)).ToDictionary();

                foreach (var match in OmniboxUtils.Matches(types, OmniboxParser.Manager.AllowedType, pattern, isPascalCase).OrderBy(ma => ma.Distance))
                {
                    var type = match.Value;
                    yield return new MapOmniboxResult { Distance = keyMatch.Distance + match.Distance, KeywordMatch = keyMatch, Type = (Type)type, TypeMatch = match };
                }
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(MapOmniboxResult);
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult
                {
                    Text =  MapMessage.Map.NiceToString() + " " + typeof(TypeEntity).NiceName(),
                    ReferencedType = resultType
                }
            };
        }
    }

    public class MapOmniboxResult : OmniboxResult
    {
        public OmniboxMatch KeywordMatch { get; set; }

        [JsonIgnore]
        public Type? Type { get; set; }


        public string? TypeName { get { return this.Type == null ? null : QueryNameJsonConverter.GetQueryKey(this.Type); } }

        public OmniboxMatch TypeMatch { get; set; }

        public override string ToString()
        {
            if (Type == null)
                return KeywordMatch.Value.ToString()!;

            return "{0} {1}".FormatWith(KeywordMatch.Value, Type.NicePluralName().ToOmniboxPascal());
        }
    }

}
