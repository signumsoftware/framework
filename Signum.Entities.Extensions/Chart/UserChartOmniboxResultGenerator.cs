using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Extensions.Properties;
using System.Text.RegularExpressions;
using Signum.Entities.Omnibox;

namespace Signum.Entities.Chart
{
    public class UserChartOmniboxResultGenerator : OmniboxResultGenerator<UserChartOmniboxResult>
    {
        public int AutoCompleteLimit = 5;

        public override IEnumerable<UserChartOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (tokenPattern != "S")
                yield break;

            string ident = OmniboxUtils.CleanCommas(tokens[0].Value);

            var userCharts = OmniboxParser.Manager.AutoComplete(typeof(UserChartDN), ident, AutoCompleteLimit);

            foreach (var uq in userCharts)
            {
                var match = OmniboxUtils.Contains(uq, uq.ToString(), ident);

                yield return new UserChartOmniboxResult
                {
                    ToStr = ident,
                    ToStrMatch = match,
                    Distance = match.Distance,
                    UserChart = (Lite<UserChartDN>)uq,
                };
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(UserChartOmniboxResult);
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult 
                { 
                    Text = "'{0}'".Formato(OmniboxMessage.Omnibox_UserChart.NiceToString()), 
                    OmniboxResultType = resultType 
                }
            };
        }
    }

    public class UserChartOmniboxResult : OmniboxResult
    {
        public string ToStr { get; set; }
        public OmniboxMatch ToStrMatch { get; set; }

        public Lite<UserChartDN> UserChart { get; set; }

        public override string ToString()
        {
            return "\"{0}\"".Formato(ToStr);
        }
    }
}
