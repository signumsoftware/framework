using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.ControlPanel;
using System.Reflection;
using Signum.Windows.ControlPanels.Admin;

namespace Signum.Windows.ControlPanels
{
    public static class ControlPanelClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<ControlPanelDN>(EntityType.Admin) { View = e => new ControlPanelEdit() },
                    new EmbeddedEntitySettings<PanelPart> { View = (e, t) => new PanelPartEdit() },

                    new EntitySettings<CountSearchControlPartDN>(EntityType.Content) { View = e => new CountSearchControlPart() },
                    new EntitySettings<LinkListPartDN>(EntityType.Content) { View = e => new LinkListPart() },
                    new EntitySettings<UserQueryPartDN>(EntityType.Content) { View = e => new UserQueryPart() },                
                    new EntitySettings<UserChartPartDN>(EntityType.Content) { View = e => new UserChartPart() }
                }); 
                
            }
        }
    }
}
