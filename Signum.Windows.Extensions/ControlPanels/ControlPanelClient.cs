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
using Signum.Windows.ControlPanels;

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
                    new EntitySettings<ControlPanelDN>() { View = e => new ControlPanelEdit() },

                    new EntitySettings<CountSearchControlPartDN>() { View = e => new CountSearchControlPartEdit() },
                    new EntitySettings<LinkListPartDN>() { View = e => new LinkListPartEdit() },
                    new EntitySettings<UserQueryPartDN>() { View = e => new UserQueryPartEdit() },                
                    new EntitySettings<UserChartPartDN>() { View = e => new UserChartPartEdit() }
                });

                PartViews.Add(typeof(UserQueryPartDN), new PartView
                {
                    ViewControl = () => new UserQueryPartView(),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserQueryPartDN)part).UserQuery);
                    }
                });

                PartViews.Add(typeof(UserChartPartDN), new PartView
                {
                    ViewControl = () => new UserChartPartView(),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserChartPartDN)part).UserChart);
                    }
                });

                PartViews.Add(typeof(CountSearchControlPartDN), new PartView
                {
                    ViewControl = () => new CountSearchControlPartView()
                });

                PartViews.Add(typeof(LinkListPartDN), new PartView
                {
                    ViewControl = () => new LinkListPartView()
                });

                LinksClient.RegisterEntityLinks<ControlPanelDN>((cp, ctrl) => new[]{
                    new QuickLinkAction(ControlPanelMessage.Preview.NiceToString(), () => ControlPanelWindow.View(cp))
                }); 
            }
        }
    }

    public class PartView
    {
        public Expression<Func<FrameworkElement>> ViewControl;
        public Action<IPartDN> OnTitleClick;
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
