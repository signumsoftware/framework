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

namespace Signum.Windows.Chart
{
    public class ChartOmniboxProvider : OmniboxProvider<ChartOmniboxResult>
    {
        public override OmniboxResultGenerator<ChartOmniboxResult> CreateGenerator()
        {
            return new ChartOmniboxResultGenerator();
        }

        public override void RenderLines(ChartOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.KeywordMatch);

            if (result.QueryNameMatch != null)
            {
                lines.Add(" ");
                lines.AddMatch(result.QueryNameMatch);
            }
            else
            {
                lines.Add(new Run(" " + OmniboxMessage.Omnibox_Query.NiceToString() + "...") { Foreground = Brushes.LightGray });
            }
        }

        public override Run GetIcon()
        {
            return new Run(" ({0})".Formato(ChartMessage.Chart.NiceToString())) { Foreground = Brushes.Violet };
        }

        public override void OnSelected(ChartOmniboxResult result, Window window)
        {
            if (result.QueryName == null)
                return;

            ChartClient.OpenChartRequest(new ChartRequest(result.QueryName), null, null);
        }

        public override string GetName(ChartOmniboxResult result)
        {
            return "C:" + result.QueryName.Try(QueryUtils.GetQueryUniqueKey);
        }
    }
}
