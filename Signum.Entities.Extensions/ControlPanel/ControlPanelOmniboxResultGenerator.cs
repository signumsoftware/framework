using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.Omnibox;

namespace Signum.Entities.ControlPanel
{
    public class ControlPanelOmniboxResultGenerator : OmniboxResultGenerator<ControlPanelOmniboxResult>
    {
        Func<string, int, IEnumerable<Lite<ControlPanelDN>>> autoComplete;

        public ControlPanelOmniboxResultGenerator(Func<string, int, IEnumerable<Lite<ControlPanelDN>>> autoComplete)
        {
            this.autoComplete = autoComplete;
        }

        public int AutoCompleteLimit = 5;

        public override IEnumerable<ControlPanelOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (tokenPattern != "S" || !OmniboxParser.Manager.AllowedPermission(ControlPanelPermission.ViewControlPanel))
                yield break;

            string ident = OmniboxUtils.CleanCommas(tokens[0].Value);

            var controlPanel = autoComplete(ident, AutoCompleteLimit);

            foreach (var uq in controlPanel)
            {
                var match = OmniboxUtils.Contains(uq, uq.ToString(), ident);

                yield return new ControlPanelOmniboxResult 
                {
                    ToStr = ident,
                    ToStrMatch = match,
                    Distance = match.Distance,
                    ControlPanel = (Lite<ControlPanelDN>)uq,
                };
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(ControlPanelOmniboxResult);
            var userQuery = OmniboxMessage.Omnibox_UserQuery.NiceToString();
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult { Text = "'{0}'".Formato(userQuery), OmniboxResultType = resultType }
            };
        }
    }

    public class ControlPanelOmniboxResult : OmniboxResult
    {
        public string ToStr { get; set; }
        public OmniboxMatch ToStrMatch { get; set; }

        public Lite<ControlPanelDN> ControlPanel { get; set; }

        public override string ToString()
        {
            return "\"{0}\"".Formato(ToStr);
        }
    }
}
