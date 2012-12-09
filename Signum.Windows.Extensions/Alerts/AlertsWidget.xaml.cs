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
using Signum.Entities.Alerts;
using Signum.Entities.Authorization;

namespace Signum.Windows.Alerts
{
    /// <summary>
    /// Interaction logic for AlertsWidget.xaml
    /// </summary>
    public partial class AlertsWidget : UserControl, IWidget
    {
        public event Action ForceShow;

        public static AlertDN CreateAlert(IdentifiableEntity entity)
        {
            if(entity.IsNew)
                return null;

            return new AlertDN
            {
                Target = entity.ToLite(),
                CreatedBy = UserDN.Current.ToLite()
            };
        }


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
                Lite<AlertDN> alert = (Lite<AlertDN>)b.Tag;
                ViewAlert(Server.RetrieveAndForget(alert));
            }
        }

        private void btnNewAlert_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            AlertDN alert = CreateAlert((IdentifiableEntity)DataContext);

            ViewAlert(alert);
        }

        private void ViewAlert(AlertDN alert)
        {
            Navigator.Navigate(alert, new NavigateOptions()
            {
                Closed = (o, e) => ReloadAlerts(),
            });
        }

        public void ReloadAlerts()
        {
            IdentifiableEntity entity = DataContext as IdentifiableEntity;
            if (entity == null || entity.IsNew)
            {
                //lvAlerts.ItemsSource = null;
                return;
            }

            tbAlerts.FontWeight = FontWeights.Normal;
            CountAlerts(entity, "Future", Properties.Resources.FutureAlerts, btnFutureAlerts);
            CountAlerts(entity, "Attended", Properties.Resources.CheckedAlerts, btnCheckedAlerts);
            CountAlerts(entity, "NotAttended", Properties.Resources.WarnedAlerts, btnWarnedAlerts);
        }

        void CountAlerts(IdentifiableEntity entity, string filterColumn, string resource, Button button)
        {
            Navigator.QueryCountBatch(new CountOptions(typeof(AlertDN))
            {
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption("Target" , DataContext),
                    new FilterOption("Entity." + filterColumn, true),
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

            string field =
                sender == btnFutureAlerts ? "Future" :
                sender == btnCheckedAlerts ? "Attended" :
                sender == btnWarnedAlerts ? "NotAttended" : null;

            Navigator.Explore(new ExploreOptions(typeof(AlertDN))
            {
                ShowFilters = false,
                SearchOnLoad = true,
                FilterOptions = 
                { 
                    new FilterOption("Target", entity) { Frozen = true }, 
                    new FilterOption("Entity." + field, true)
                },
                ColumnOptions = { new ColumnOption("Target") },
                ColumnOptionsMode = ColumnOptionsMode.Remove,
                Closed = (o, ea) => ReloadAlerts(),
            });
        }
    }
}
