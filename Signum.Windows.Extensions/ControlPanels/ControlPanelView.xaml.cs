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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Entities.ControlPanel;
using Signum.Services;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows.ControlPanels
{
    /// <summary>
    /// Interaction logic for ControlPanelView.xaml
    /// </summary>
    public partial class ControlPanelView : UserControl
    {
        public ControlPanelDN Current
        {
            get { return (ControlPanelDN)DataContext; }
            set { DataContext = value; }
        }

        public ControlPanelView()
        {
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

        private void cpCombo_EntityChanged(object sender, bool userInteraction, object oldValue, object newValue)
        {
            if (userInteraction )
            {
                Lite<ControlPanelDN> cp = newValue as Lite<ControlPanelDN>;
                Current = cp == null ? null : cp.RetrieveAndForget();
            }

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

        private void TextBlock_MouseUp_1(object sender, MouseButtonEventArgs e)
        {

        }


        private void GroupBox_Loaded(object sender, RoutedEventArgs e)
        {
            GroupBox gb = (GroupBox)sender;
            PanelPart pp = (PanelPart)gb.DataContext;
            PartView pv = ControlPanelClient.PartViews.GetOrThrow(pp.Content.GetType());
            if (pv.OnTitleClick != null)
            {
                TextBlock tb = (TextBlock)gb.FindName("tb");
                tb.Cursor = Cursors.Hand;
                tb.MouseDown += TextBlock_MouseUp;
            }
        }


        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            PanelPart pp = (PanelPart)tb.DataContext;
            ControlPanelClient.PartViews.GetOrThrow(pp.Content.GetType()).OnTitleClick(pp.Content);
        }

       
    }
}
