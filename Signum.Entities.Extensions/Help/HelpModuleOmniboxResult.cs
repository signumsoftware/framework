using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Omnibox;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Authorization;

namespace Signum.Entities.Help
{
    public class HelpModuleOmniboxResultGenerator : OmniboxResultGenerator<HelpModuleOmniboxResult>
    {
        public Func<string> NiceName = () => OmniboxMessage.Omnibox_Help.NiceToString();

        public override IEnumerable<HelpModuleOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (!OmniboxParser.Manager.AllowedPermission(HelpPermissions.ViewHelp))
                yield break;

            if (tokens.Count == 0 || !tokens.All(a=>a.Type == OmniboxTokenType.Identifier))
                yield break;

            string key = tokens[0].Value;

            var keyMatch = OmniboxUtils.Contains(NiceName(), NiceName(), key) ?? OmniboxUtils.Contains("help", "help", key);

            if (keyMatch == null)
                yield break;

            if (tokenPattern == "I" && rawQuery.EndsWith(" "))
            {
                yield return new HelpModuleOmniboxResult { Distance = keyMatch.Distance, KeywordMatch = keyMatch, IsIndex = false, SecondMatch = null };
                yield break;
            }

            if(tokens.Count != 2)
                yield break;

            string pattern = tokens[1].Value;

            var indexMatch = OmniboxUtils.Contains(HelpMessage.Index.NiceToString(), HelpMessage.Index.NiceToString(), pattern);
            if (indexMatch != null)
                yield return new HelpModuleOmniboxResult { Distance = keyMatch.Distance + indexMatch.Distance, KeywordMatch = keyMatch, IsIndex = true, SecondMatch = indexMatch };

            bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

            foreach (var match in OmniboxUtils.Matches(OmniboxParser.Manager.Types(), OmniboxParser.Manager.AllowedType, pattern, isPascalCase).OrderBy(ma => ma.Distance))
            {
                var type = (Type)match.Value;
                if (OmniboxParser.Manager.AllowedQuery(type))
                {
                    yield return new HelpModuleOmniboxResult { Distance = keyMatch.Distance + match.Distance, KeywordMatch = keyMatch, Type = type, SecondMatch = match };
                }
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(HelpModuleOmniboxResult);
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult 
                { 
                    Text =  NiceName() + " " + HelpMessage.Index.ToString(), 
                    OmniboxResultType = resultType,
                },
                new HelpOmniboxResult 
                { 
                    Text =  NiceName() + " " + typeof(TypeDN).NiceName(), 
                    OmniboxResultType = resultType 
                },
            };
        }
    }

    public class HelpModuleOmniboxResult : OmniboxResult
    {
        public OmniboxMatch KeywordMatch { get; set; }

        public bool IsIndex { get; set; }
        public Type Type { get; set; }
        public OmniboxMatch SecondMatch { get; set; }

        public override string ToString()
        {
            if (Type == null && !IsIndex)
                return KeywordMatch.Value.ToString();

            return "{0} {1}".Formato(KeywordMatch.Value,
                IsIndex ? HelpMessage.Index.ToString() :
                Type.NiceName().ToOmniboxPascal());
        }
    }

    public static class HelpPermissions
    {
        public static readonly PermissionSymbol ViewHelp = new PermissionSymbol();
    }
}
