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
        public static Func<IdentifiableEntity, List<Lite<IAlertDN>>> RetrieveAlerts { get; set; }
        public static Action<IdentifiableEntity, AlertsWidget> WarnedAlerts { get; set; }
        public static Action<IdentifiableEntity, AlertsWidget> CheckedAlerts { get; set; }
        public static Action<IdentifiableEntity, AlertsWidget> FutureAlerts { get; set; }
        public static Func<IdentifiableEntity, CountAlerts> CountAlerts { get; set; }

        public AlertsWidget()
        {
            InitializeComponent();

            //lvAlerts.AddHandler(Button.ClickEvent, new RoutedEventHandler(Alert_MouseDown));
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(AlertsWidget_DataContextChanged);
        }

        private void AlertsWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
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
                throw new ApplicationException("AlertsWidget.CreateAlert is null");

            if (DataContext == null)
                return;

            IAlertDN alert = CreateAlert((IdentifiableEntity)DataContext);

            ViewAlert(alert);
        }

        private void ViewAlert(IAlertDN alert)
        {
            Navigator.Navigate(alert, new NavigateOptions
            {
                Closed = (o, e) => ReloadAlerts(),
            });
        }

        public void ReloadAlerts()
        {
            if (CreateAlert == null)
                throw new ApplicationException("AlertsWidget.RetrieveAlerts is null");

            IdentifiableEntity entity = DataContext as IdentifiableEntity;
            if (entity == null || entity.IsNew)
            {
                //lvAlerts.ItemsSource = null;
                return;
            }

            List<Lite<IAlertDN>> alerts = RetrieveAlerts((IdentifiableEntity)DataContext);

            if (alerts != null)
            {
                tbAlerts.FontWeight = alerts.Count == 0 ? FontWeights.Normal : FontWeights.Bold;

                if (alerts.Count > 0 && ForceShow != null)
                    ForceShow();
            }

            //lvAlerts.ItemsSource = alerts;

            CountAlerts count = CountAlerts(entity);

            if (count == null || count.CheckedAlerts == 0)
            {
                btnCheckedAlerts.Visibility = Visibility.Collapsed;
                btnCheckedAlerts.Content = "{0} (0)".Formato(Properties.Resources.CheckedAlerts);
            }
            else
            {
                btnCheckedAlerts.Visibility = Visibility.Visible;
                btnCheckedAlerts.Content = "{0} ({1})".Formato(Properties.Resources.CheckedAlerts, count.CheckedAlerts);
            }

            if (count == null || count.WarnedAlerts == 0)
            {
                btnWarnedAlerts.Visibility = Visibility.Collapsed;
                btnWarnedAlerts.Content = "{0} (0)".Formato(Properties.Resources.WarnedAlerts);
            }
            else
            {
                btnWarnedAlerts.Visibility = Visibility.Visible;
                btnWarnedAlerts.Content = "{0} ({1})".Formato(Properties.Resources.WarnedAlerts, count.WarnedAlerts);
            }

            if (count == null || count.FutureAlerts == 0)
            {
                btnFutureAlerts.Visibility = Visibility.Collapsed;
                btnFutureAlerts.Content = "{0} (0)".Formato(Properties.Resources.FutureAlerts);
            }
            else
            {
                btnFutureAlerts.Visibility = Visibility.Visible;
                btnFutureAlerts.Content = "{0} ({1})".Formato(Properties.Resources.FutureAlerts, count.FutureAlerts);
            }

        }

        private void btnAlertsWarn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            IdentifiableEntity entity = DataContext as IdentifiableEntity;

            WarnedAlerts(entity, this);
        }

        private void btnAlertsChecked_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            IdentifiableEntity entity = DataContext as IdentifiableEntity;

            CheckedAlerts(entity, this);
        }

        private void btnFutureAlerts_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            IdentifiableEntity entity = DataContext as IdentifiableEntity;

            FutureAlerts(entity, this);
        }


    }
}
