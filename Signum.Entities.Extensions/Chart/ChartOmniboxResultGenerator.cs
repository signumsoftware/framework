using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using System.Text.RegularExpressions;
using Signum.Entities.Omnibox;
using Signum.Entities.DynamicQuery;
using Newtonsoft.Json;

namespace Signum.Entities.Chart
{
    public class ChartOmniboxResultGenerator : OmniboxResultGenerator<ChartOmniboxResult>
    {
        public Func<string> NiceName = () => ChartMessage.ChartToken.NiceToString();

        Regex regex = new Regex(@"^II?$", RegexOptions.ExplicitCapture);
        public override IEnumerable<ChartOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (!OmniboxParser.Manager.AllowedPermission(ChartPermission.ViewCharting))
                yield break;

            Match m = regex.Match(tokenPattern);

            if (!m.Success)
                yield break;

            string key = tokens[0].Value;

            var keyMatch = OmniboxUtils.Contains(NiceName(), NiceName(), key);

            if (keyMatch == null)
                yield break;

            if (tokens.Count == 1)
                yield return new ChartOmniboxResult { Distance = keyMatch.Distance, KeywordMatch = keyMatch };

            else
            {
                string pattern = tokens[1].Value;

                bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

                foreach (var match in OmniboxUtils.Matches(OmniboxParser.Manager.GetQueries(), OmniboxParser.Manager.AllowedQuery, pattern, isPascalCase).OrderBy(ma => ma.Distance))
                {
                    yield return new ChartOmniboxResult
                    {
                        Distance = keyMatch.Distance + match.Distance,
                        KeywordMatch = keyMatch,
                        QueryName = match.Value,
                        QueryNameMatch = match
                    };
                }
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(ChartOmniboxResult);
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult
                {
                    Text =  ChartMessage.ChartToken.NiceToString() + " " + OmniboxMessage.Omnibox_Query.NiceToString(),
                    ReferencedType = resultType
                }
            };
        }
    }

    public class ChartOmniboxResult : OmniboxResult
    {
        public OmniboxMatch KeywordMatch { get; set; }

        [JsonConverter(typeof(QueryNameJsonConverter))]
        public object QueryName { get; set; }
        public OmniboxMatch QueryNameMatch { get; set; }

        public override string ToString()
        {
            if (QueryName == null)
                return KeywordMatch.Value.ToString()!;

            return "{0} {1}".FormatWith(KeywordMatch.Value, QueryUtils.GetNiceName(QueryName).ToOmniboxPascal());
        }
    }
}
