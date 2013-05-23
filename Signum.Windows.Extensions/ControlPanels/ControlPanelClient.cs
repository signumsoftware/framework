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
using Signum.Windows.Basics;
using Signum.Entities.UserQueries;
using Signum.Entities.Reflection;
using Signum.Services;
using Signum.Windows.Authorization;
using Signum.Entities.Chart;
using Signum.Windows.UserQueries;

namespace Signum.Windows.ControlPanels
{
    public static class ControlPanelClient
    {
        public static Dictionary<Type, PartView> PartViews = new Dictionary<Type, PartView>();

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                TypeClient.Start();

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<ControlPanelDN>();
                UserAssetsClient.RegisterExportAssertLink<LinkListPartDN>();
                UserAssetsClient.RegisterExportAssertLink<CountSearchControlPartDN>();
                UserAssetsClient.RegisterExportAssertLink<UserQueryPartDN>();
                UserAssetsClient.RegisterExportAssertLink<UserChartPartDN>();

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<ControlPanelDN>() { View = e => new ControlPanelEdit(), Icon = ExtensionsImageLoader.GetImageSortName("controlPanel.png") },

                    new EntitySettings<CountSearchControlPartDN>() { View = e => new CountSearchControlPartEdit() },
                    new EntitySettings<LinkListPartDN>() { View = e => new LinkListPartEdit() },
                    new EntitySettings<UserQueryPartDN>() { View = e => new UserQueryPartEdit() },                
                    new EntitySettings<UserChartPartDN>() { View = e => new UserChartPartEdit() }
                });

                PartViews.Add(typeof(UserQueryPartDN), new PartView
                {
                    ViewControl = () => new UserQueryPartView(),
                    IsTitleEnabled = () => Navigator.IsNavigable(typeof(UserQueryDN), true),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserQueryPartDN)part).UserQuery);
                    }
                });

                PartViews.Add(typeof(UserChartPartDN), new PartView
                {
                    ViewControl = () => new UserChartPartView(),
                    IsTitleEnabled = ()=> Navigator.IsNavigable(typeof(UserChartDN), true),
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
                    !ControlPanelPermission.ViewControlPanel.IsAuthorized() ? null:  
                    new QuickLinkAction(ControlPanelMessage.Preview.NiceToString(), () => View(cp, null))
                });

                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                {       
                    if(!ControlPanelPermission.ViewControlPanel.IsAuthorized())
                        return null;

                    return Server.Return((IControlPanelServer us) => us.GetControlPanelsEntity(entity.EntityType))
                        .Select(cp => new ControlPanelQuickLink(cp, entity)).ToArray();
                });
            }
        }

        class ControlPanelQuickLink : QuickLink
        {
            Lite<ControlPanelDN> controlPanel;
            Lite<IdentifiableEntity> entity;

            public ControlPanelQuickLink(Lite<ControlPanelDN> controlPanel, Lite<IdentifiableEntity> entity)
            {
                this.ToolTip = controlPanel.ToString(); 
                this.Label = controlPanel.ToString();
                this.controlPanel = controlPanel;
                this.entity = entity;
                this.IsVisible = true;
                this.Icon = ExtensionsImageLoader.GetImageSortName("controlPanel.png");
            }

            public override void Execute()
            {
                ControlPanelClient.View(controlPanel, entity.Retrieve());
            }
        }

        public static void View(Lite<ControlPanelDN> controlPanel, IdentifiableEntity currentEntity)
        {
            ControlPanelWindow win = new ControlPanelWindow();

            win.tbControlPanel.Text = NormalWindowMessage.Loading0.NiceToString().Formato(controlPanel.EntityType.NiceName());
            win.Show();

            var cp = controlPanel.Retrieve();

            if (cp.EntityType != null)
            {
                var filters = GraphExplorer.FromRoot(cp).OfType<QueryFilterDN>();

                CurrentEntityConverter.SetFilterValues(filters, currentEntity);

                win.CurrentEntity = currentEntity;
            }

            win.DataContext = controlPanel.Retrieve();
        }
    }

    public class PartView
    {
        public Expression<Func<FrameworkElement>> ViewControl;
        public Action<IPartDN> OnTitleClick;
        public Func<bool> IsTitleEnabled;
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
