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

namespace Signum.Windows.Chart
{
    public class UserChartOmniboxProvider : OmniboxProvider<UserChartOmniboxResult>
    {
        public override OmniboxResultGenerator<UserChartOmniboxResult> CreateGenerator()
        {
            return new UserChartOmniboxResultGenerator();
        }

        public override void RenderLines(UserChartOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.ToStrMatch);

            lines.Add(new Run(" ({0})".Formato(typeof(UserChartDN).NiceName())) { Foreground = Brushes.DarkViolet });
        }

        public override void OnSelected(UserChartOmniboxResult result)
        {
            UserChartDN uc = result.UserChart.RetrieveAndForget();

            var query = QueryClient.queryNames[uc.Query.Key];

            ChartWindow window = new ChartWindow()
            {
                DataContext = new ChartRequest(query)
            };

            ChartClient.SetUserChart(window, uc);

            window.Show();
        }
    }
}
