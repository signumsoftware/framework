using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Omnibox;
using Signum.Entities.UserQueries;
using Signum.Entities.Omnibox;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Utilities;
using Signum.Windows.Authorization;
using Signum.Entities.Chart;
using Signum.Entities.DynamicQuery;
using System.Windows;
using Signum.Services;

namespace Signum.Windows.Chart
{
    public class UserChartOmniboxProvider : OmniboxProvider<UserChartOmniboxResult>
    {
        public override OmniboxResultGenerator<UserChartOmniboxResult> CreateGenerator()
        {
            return new UserChartOmniboxResultGenerator((subString, limit) => Server.Return((IChartServer cs) => cs.AutocompleteUserChart(subString, limit)));
        }

        public override void RenderLines(UserChartOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.ToStrMatch);
        }

        public override Run GetIcon()
        {
            return new Run("({0})".FormatWith(typeof(UserChartEntity).NiceName())) { Foreground = Brushes.DarkViolet };
        }

        public override void OnSelected(UserChartOmniboxResult result, Window window)
        {
            UserChartEntity uc = result.UserChart.RetrieveAndForget();

            ChartClient.View(uc, null);
        }

        public override string GetName(UserChartOmniboxResult result)
        {
            return "UC:" + result.UserChart.Key();
        }
    }
}
