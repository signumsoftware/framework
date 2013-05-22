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
using Signum.Entities.ControlPanel;
using Signum.Services;
using Signum.Utilities;
using Signum.Windows.Authorization;

namespace Signum.Windows.ControlPanels
{
    /// <summary>
    /// Interaction logic for ControlPanelSelector.xaml
    /// </summary>
    public partial class ControlPanelSelector : Window
    {
        public ControlPanelDN Current
        {
            get { return (ControlPanelDN)borderControlPanel.DataContext; }
            set { borderControlPanel.DataContext = value; }
        }

        public ControlPanelSelector()
        {
            ControlPanelPermission.ViewControlPanel.Authorize();
            InitializeComponent();
            
            cpCombo.LoadData += cpCombo_LoadData;
            cpCombo.Implementations = Implementations.By(typeof(ControlPanelDN));
            cpCombo.Type = typeof(Lite<ControlPanelDN>);
            cpCombo.LabelText = typeof(ControlPanelDN).NiceName();
            cpCombo.Create = Navigator.IsCreable(typeof(ControlPanelDN));
            cpCombo.View = Navigator.IsViewable(typeof(ControlPanelDN));
            cpCombo.Find = false;
            cpCombo.Remove = false;
            this.Loaded += new RoutedEventHandler(ControlPanelView_Loaded);
        }

        IEnumerable<Lite<IIdentifiable>> cpCombo_LoadData()
        {
            return Server.RetrieveAllLite<ControlPanelDN>();
        }

        void ControlPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            Current = Server.Return((IControlPanelServer cps) => cps.GetHomePageControlPanel());
            cpCombo.Entity = Current.ToLite();
        }

        private void reload_Click(object sender, RoutedEventArgs e)
        {
            cpCombo.LoadNow();

            if (Current != null)
            {
                var lite = Current.ToLite();
                Current = null;
                Current = lite.Retrieve();
            }

            cpCombo.Entity = Current.ToLite();
        }

        private void cpCombo_EntityChanged(object sender, bool userInteraction, object oldValue, object newValue)
        {
            if (userInteraction)
            {
                Lite<ControlPanelDN> cp = newValue as Lite<ControlPanelDN>;
                Current = cp == null ? null : cp.RetrieveAndForget();
            }

        }
    }
}
