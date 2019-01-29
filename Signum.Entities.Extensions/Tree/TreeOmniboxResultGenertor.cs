using Newtonsoft.Json;
using Signum.Entities.Omnibox;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Entities.Tree
{
    public class TreeOmniboxResultGenerator : OmniboxResultGenerator<TreeOmniboxResult>
    {
        Regex regex = new Regex(@"^II?$", RegexOptions.ExplicitCapture);
        public override IEnumerable<TreeOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (tokenPattern != "I")
                yield break;
            
            string pattern = tokens[0].Value;

            bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

            foreach (var match in OmniboxUtils.Matches(OmniboxParser.Manager.Types(), t => typeof(TreeEntity).IsAssignableFrom(t) && OmniboxParser.Manager.AllowedType(t) && OmniboxParser.Manager.AllowedQuery(t), pattern, isPascalCase).OrderBy(ma => ma.Distance))
            {
                var type = match.Value;

                yield return new TreeOmniboxResult
                {
                    Distance = match.Distance - 0.1f,
                    Type = (Type)match.Value,
                    TypeMatch = match
                };
            }

        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(TreeOmniboxResult);
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult
                {
                    Text = TreeMessage.TreeType.NiceToString(),
                    ReferencedType = resultType
                }
            };
        }
    }

    public class TreeOmniboxResult : OmniboxResult
    {
        [JsonConverter(typeof(QueryNameJsonConverter))]
        public Type Type { get; set; }
        public OmniboxMatch TypeMatch { get; set; }

        public override string ToString()
        {
            return Type.NicePluralName().ToOmniboxPascal();
        }
    }
}
