using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Signum.Entities;
using Signum.Entities.Dashboard;
using Signum.Services;
using Signum.Utilities;
using Signum.Windows.Authorization;

namespace Signum.Windows.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardSelector.xaml
    /// </summary>
    public partial class DashboardSelector : Window
    {
        public DashboardEntity Current
        {
            get { return (DashboardEntity)borderDashboard.DataContext; }
            set { borderDashboard.DataContext = value; }
        }

        public DashboardSelector()
        {
            DashboardPermission.ViewDashboard.Authorize();
            InitializeComponent();
            
            cpCombo.LoadData += cpCombo_LoadData;
            cpCombo.Implementations = Implementations.By(typeof(DashboardEntity));
            cpCombo.Type = typeof(Lite<DashboardEntity>);
            cpCombo.LabelText = typeof(DashboardEntity).NiceName();
            cpCombo.Create = Navigator.IsCreable(typeof(DashboardEntity));
            cpCombo.View = Navigator.IsViewable(typeof(DashboardEntity));
            cpCombo.Find = false;
            cpCombo.Remove = false;
            this.Loaded += new RoutedEventHandler(DashboardView_Loaded);
        }

        IEnumerable<Lite<IEntity>> cpCombo_LoadData()
        {
            return Server.Return((IDashboardServer cps) => cps.GetDashboards());
        }

        void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Current = Server.Return((IDashboardServer cps) => cps.GetHomePageDashboard());
            cpCombo.Entity = Current.ToLite();
        }

        private void reload_Click(object sender, RoutedEventArgs e)
        {
            cpCombo.LoadNow();

            if (Current != null)
            {
                var lite = Current.ToLite();
                Current = null;
                Current = Server.Return((IDashboardServer cps) => cps.RetrieveDashboard(lite));
            }

            cpCombo.Entity = Current.ToLite();
        }

        private void cpCombo_EntityChanged(object sender, bool userInteraction, object oldValue, object newValue)
        {
            if (userInteraction)
            {
                Lite<DashboardEntity> cp = newValue as Lite<DashboardEntity>;
                Current = cp == null ? null : Server.Return((IDashboardServer cps) => cps.RetrieveDashboard(cp));
            }

        }
    }
}
