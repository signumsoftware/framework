using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.ControlPanel;
using System.Reflection;

namespace Signum.Windows.ControlPanels
{
    public static class ControlPanelClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ControlPanelDN>(EntityType.Admin) { View = e => new ControlPanelEdit() });
            }
        }
    }
}
