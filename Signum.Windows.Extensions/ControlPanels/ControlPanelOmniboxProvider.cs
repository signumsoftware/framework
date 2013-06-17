using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Omnibox;
using Signum.Entities.ControlPanel;
using Signum.Entities.Omnibox;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Utilities;
using Signum.Windows.Authorization;
using System.Windows;
using Signum.Windows.ControlPanels;
using Signum.Services;

namespace Signum.Windows.ControlPanels
{
    public class ControlPanelOmniboxProvider: OmniboxProvider<ControlPanelOmniboxResult>
    {
        public override OmniboxResultGenerator<ControlPanelOmniboxResult> CreateGenerator()
        {
            return new ControlPanelOmniboxResultGenerator((subString,  limit) => Server.Return((IControlPanelServer s) => s.AutocompleteControlPanel(subString, limit)));
        }

        public override void RenderLines(ControlPanelOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.ToStrMatch);
        }

        public override Run GetIcon()
        {
            return new Run("({0})".Formato(typeof(ControlPanelDN).NiceName())) { Foreground = Brushes.DarkSlateBlue };
        }

        public override void OnSelected(ControlPanelOmniboxResult result, Window window)
        {
            ControlPanelClient.Navigate(result.ControlPanel, null);
        }

        public override string GetName(ControlPanelOmniboxResult result)
        {
            return "CP:" + result.ControlPanel.Key();
        }
    }
}
