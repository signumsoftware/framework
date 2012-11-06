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
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Services;

namespace Signum.Windows.Widgets
{
    /// <summary>
    /// Interaction logic for AlertsWidget.xaml
    /// </summary>
    public partial class AlertsWidget : UserControl, IWidget
    {

        #region IWidget Members

        public event Action ForceShow;

        #endregion

        public static Func<IdentifiableEntity, IAlertDN> CreateAlert { get; set; }
        public static object WarnedAlertsQuery { get; set; }
        public static object CheckedAlertsQuery { get; set; }
        public static object FutureAlertsQuery { get; set; }
        public static string AlertsQueryColumn { get; set; }

        public AlertsWidget()
        {
            InitializeComponent();

            //lvAlerts.AddHandler(Button.ClickEvent, new RoutedEventHandler(Alert_MouseDown));
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(AlertsWidget_DataContextChanged);
        }

        private void AlertsWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
                ReloadAlerts();
        }

        private void Alert_MouseDown(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button) //Not to capture the mouseDown of the scrollbar buttons
            {
                Button b = (Button)e.OriginalSource;
                Lite<IAlertDN> alert = (Lite<IAlertDN>)b.Tag;
                ViewAlert(Server.RetrieveAndForget(alert));
            }
        }

        private void btnNewAlert_Click(object sender, RoutedEventArgs e)
        {
            if (CreateAlert == null)
                throw new ArgumentNullException("AlertsWidget.CreateAlert");

            if (DataContext == null)
                return;

            IAlertDN alert = CreateAlert((IdentifiableEntity)DataContext);

            ViewAlert(alert);
        }

        private void ViewAlert(IAlertDN alert)
        {
            Navigator.Navigate(alert, new NavigateOptions()
            {
                Closed = (o, e) => ReloadAlerts(),
            });
        }

        public void ReloadAlerts()
        {
            if (CreateAlert == null)
                throw new ArgumentNullException("AlertsWidget.RetrieveAlerts");

            IdentifiableEntity entity = DataContext as IdentifiableEntity;
            if (entity == null || entity.IsNew)
            {
                //lvAlerts.ItemsSource = null;
                return;
            }

            tbAlerts.FontWeight = FontWeights.Normal;
            CountAlerts(FutureAlertsQuery, entity, Properties.Resources.FutureAlerts, btnFutureAlerts);
            CountAlerts(CheckedAlertsQuery, entity, Properties.Resources.CheckedAlerts, btnCheckedAlerts);
            CountAlerts(WarnedAlertsQuery, entity, Properties.Resources.WarnedAlerts, btnWarnedAlerts);
        }

        void CountAlerts(object queryName, IdentifiableEntity entity, string resource, Button button)
        {
            Navigator.QueryCountBatch(new CountOptions(queryName)
            {
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption( AlertsQueryColumn , DataContext)
                }
            },
            count =>
            {
                if (count == 0)
                {
                    button.Visibility = Visibility.Collapsed;
                }
                else
                {
                    button.Visibility = Visibility.Visible;
                    button.Content = "{0} ({1})".Formato(resource, count);

                    tbAlerts.FontWeight = FontWeights.Bold;

                    if (ForceShow != null)
                        ForceShow();
                }
            }, () => { }); 
        }

        private void btnAlerts_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            IdentifiableEntity entity = DataContext as IdentifiableEntity;

            object queryName = 
                sender == btnFutureAlerts? FutureAlertsQuery: 
                sender == btnCheckedAlerts? CheckedAlertsQuery: 
                sender == btnWarnedAlerts? WarnedAlertsQuery: null;

            Navigator.Explore(new ExploreOptions(queryName)
            {
                ShowFilters = false,
                SearchOnLoad = true,
                FilterOptions = { new FilterOption(AlertsQueryColumn, entity) { Frozen = true } },
                ColumnOptions = { new ColumnOption(AlertsQueryColumn)},
                ColumnOptionsMode = ColumnOptionsMode.Remove,
                Closed = (o, ea) => ReloadAlerts(),
            });
        }
    }
}
