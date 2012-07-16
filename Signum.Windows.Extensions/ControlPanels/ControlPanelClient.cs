using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.ControlPanel;
using System.Reflection;
using Signum.Windows.ControlPanels.Admin;
using System.Windows.Controls;
using Signum.Entities;
using System.Windows;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Windows.ControlPanels
{
    public static class ControlPanelClient
    {
        public static Dictionary<Type, PartView> PartViews = new Dictionary<Type, PartView>();

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<ControlPanelDN>(EntityType.Admin) { View = e => new ControlPanelEdit() },

                    new EntitySettings<CountSearchControlPartDN>(EntityType.Content) { View = e => new CountSearchControlPartEdit() },
                    new EntitySettings<LinkListPartDN>(EntityType.Content) { View = e => new LinkListPartEdit() },
                    new EntitySettings<UserQueryPartDN>(EntityType.Content) { View = e => new UserQueryPartEdit() },                
                    new EntitySettings<UserChartPartDN>(EntityType.Content) { View = e => new UserChartPartEdit() }
                });

                PartViews.Add(typeof(CountSearchControlPartDN), new PartView { ViewControl = () => new CountSearchControlPartView() });
                PartViews.Add(typeof(UserQueryPartDN), new PartView { ViewControl = () => new UserQueryPartView() });
                PartViews.Add(typeof(LinkListPartDN), new PartView { ViewControl = () => new LinkListPartView() });
            }
        }
    }

    public class PartView
    {
        public Expression<Func<FrameworkElement>> ViewControl;
        public Action<IPanelPartContent, Control> OnTitleClick;
    }

    public class ControlPanelViewDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            PartView pv = ControlPanelClient.PartViews.TryGetC(item.GetType());

            if (pv == null)
                return null;

            return Fluent.GetDataTemplate(pv.ViewControl);
        }
    }
}
