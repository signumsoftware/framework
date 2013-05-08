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

namespace Signum.Windows.ControlPanels
{
    public class ControlPanelOmniboxProvider: OmniboxProvider<ControlPanelOmniboxResult>
    {
        public override OmniboxResultGenerator<ControlPanelOmniboxResult> CreateGenerator()
        {
            return new ControlPanelOmniboxResultGenerator();
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
            ControlPanelWindow.View(result.ControlPanel);
        }

        public override string GetName(ControlPanelOmniboxResult result)
        {
            return "CP:" + result.ControlPanel.Key();
        }
    }
}
