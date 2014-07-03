using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.Omnibox;

namespace Signum.Entities.Dashboard
{
    public class DashboardOmniboxResultGenerator : OmniboxResultGenerator<DashboardOmniboxResult>
    {
        Func<string, int, IEnumerable<Lite<DashboardDN>>> autoComplete;

        public DashboardOmniboxResultGenerator(Func<string, int, IEnumerable<Lite<DashboardDN>>> autoComplete)
        {
            this.autoComplete = autoComplete;
        }

        public int AutoCompleteLimit = 5;

        public override IEnumerable<DashboardOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (tokenPattern != "S" || !OmniboxParser.Manager.AllowedPermission(DashboardPermission.ViewDashboard))
                yield break;

            string ident = OmniboxUtils.CleanCommas(tokens[0].Value);

            var dashboard = autoComplete(ident, AutoCompleteLimit);

            foreach (var uq in dashboard)
            {
                var match = OmniboxUtils.Contains(uq, uq.ToString(), ident);

                yield return new DashboardOmniboxResult 
                {
                    ToStr = ident,
                    ToStrMatch = match,
                    Distance = match.Distance,
                    Dashboard = (Lite<DashboardDN>)uq,
                };
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(DashboardOmniboxResult);
            var userQuery = OmniboxMessage.Omnibox_Dashboard.NiceToString();
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult { Text = "'{0}'".Formato(userQuery), OmniboxResultType = resultType }
            };
        }
    }

    public class DashboardOmniboxResult : OmniboxResult
    {
        public string ToStr { get; set; }
        public OmniboxMatch ToStrMatch { get; set; }

        public Lite<DashboardDN> Dashboard { get; set; }

        public override string ToString()
        {
            return "\"{0}\"".Formato(ToStr);
        }
    }
}
