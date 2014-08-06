using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.Chart;
using Signum.Entities.Omnibox;
using Signum.Entities.DynamicQuery;

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
                    var queryName = match.Value;
                    if (OmniboxParser.Manager.AllowedQuery(queryName))
                    {
                        yield return new ChartOmniboxResult { Distance = keyMatch.Distance + match.Distance, KeywordMatch = keyMatch, QueryName = queryName, QueryNameMatch = match };
                    }
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
                    OmniboxResultType = resultType 
                }
            };
        }
    }

    public class ChartOmniboxResult : OmniboxResult
    {
        public OmniboxMatch KeywordMatch { get; set; }

        public object QueryName { get; set; }
        public OmniboxMatch QueryNameMatch { get; set; }

        public override string ToString()
        {
            if (QueryName == null)
                return KeywordMatch.Value.ToString();

            return "{0} {1}".Formato(KeywordMatch.Value, QueryUtils.GetNiceName(QueryName).ToOmniboxPascal());
        }
    }
}
