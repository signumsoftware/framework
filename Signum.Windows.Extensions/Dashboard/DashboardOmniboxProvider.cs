using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Omnibox;
using Signum.Entities.Dashboard;
using Signum.Entities.Omnibox;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Utilities;
using Signum.Windows.Authorization;
using System.Windows;
using Signum.Windows.Dashboard;
using Signum.Services;

namespace Signum.Windows.Dashboard
{
    public class DashboardOmniboxProvider: OmniboxProvider<DashboardOmniboxResult>
    {
        public override OmniboxResultGenerator<DashboardOmniboxResult> CreateGenerator()
        {
            return new DashboardOmniboxResultGenerator((subString,  limit) => Server.Return((IDashboardServer s) => s.AutocompleteDashboard(subString, limit)));
        }

        public override void RenderLines(DashboardOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.ToStrMatch);
        }

        public override Run GetIcon()
        {
            return new Run("({0})".FormatWith(typeof(DashboardEntity).NiceName())) { Foreground = Brushes.DarkSlateBlue };
        }

        public override void OnSelected(DashboardOmniboxResult result, Window window)
        {
            DashboardClient.Navigate(result.Dashboard, null);
        }

        public override string GetName(DashboardOmniboxResult result)
        {
            return "CP:" + result.Dashboard.Key();
        }
    }
}
