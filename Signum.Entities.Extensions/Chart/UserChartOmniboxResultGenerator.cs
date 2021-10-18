using System;
using System.Collections.Generic;
using Signum.Utilities;
using Signum.Entities.Omnibox;

namespace Signum.Entities.Chart
{
    public class UserChartOmniboxResultGenerator : OmniboxResultGenerator<UserChartOmniboxResult>
    {
        Func<string, int, IEnumerable<Lite<UserChartEntity>>> autoComplete;

        public UserChartOmniboxResultGenerator(Func<string, int, IEnumerable<Lite<UserChartEntity>>> autoComplete)
        {
            this.autoComplete = autoComplete;
        }

        public int AutoCompleteLimit = 5;

        public override IEnumerable<UserChartOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (tokenPattern != "S" || !OmniboxParser.Manager.AllowedPermission(ChartPermission.ViewCharting))
                yield break;

            string ident = OmniboxUtils.CleanCommas(tokens[0].Value);

            var userCharts = autoComplete(ident, AutoCompleteLimit);

            foreach (var uq in userCharts)
            {
                var match = OmniboxUtils.Contains(uq, uq.ToString()!, ident)!;

                yield return new UserChartOmniboxResult
                {
                    ToStr = ident,
                    ToStrMatch = match,
                    Distance = match.Distance,
                    UserChart = (Lite<UserChartEntity>)uq,
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
                    Text = "'{0}'".FormatWith(OmniboxMessage.Omnibox_UserChart.NiceToString()),
                    ReferencedType = resultType
                }
            };
        }
    }

    public class UserChartOmniboxResult : OmniboxResult
    {
        public string ToStr { get; set; }
        public OmniboxMatch ToStrMatch { get; set; }

        public Lite<UserChartEntity>? UserChart { get; set; }

        public override string ToString()
        {
            return "\"{0}\"".FormatWith(ToStr);
        }
    }
}
