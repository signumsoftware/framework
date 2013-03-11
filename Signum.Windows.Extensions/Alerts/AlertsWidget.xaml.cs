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

            CountAlerts(entity);
        }

        public static Polymorphic<Func<IdentifiableEntity, Lite<IIdentifiable>>> FilterByAditionalData = new Polymorphic<Func<IdentifiableEntity, Lite<IIdentifiable>>>();


        private FilterOption GetFilter()
        {
            var func = FilterByAditionalData.TryGetValue(DataContext.GetType());

            if (func == null)
                return new FilterOption("Target", (IdentifiableEntity)DataContext) { Frozen = true };

            return new FilterOption("AditionalData", func((IdentifiableEntity)DataContext)) { Frozen = true };
        }

        void CountAlerts(IdentifiableEntity entity)
        {
            DynamicQueryServer.QueryGroupBatch(new QueryGroupOptions(typeof(AlertDN))
            {
                FilterOptions = new List<FilterOption>
                {
                    GetFilter(),
                },
                ColumnOptions = new List<ColumnOption>
                {
                    new ColumnOption("Entity.CurrentState"),
                    new ColumnOption("Count")
                },
                OrderOptions = new List<OrderOption>
                {
                    new OrderOption("Entity.CurrentState"),
                }
            },
            resultTable =>
            {
                if (resultTable.Rows.Length == 0)
                {
                    icAlerts.Visibility = Visibility.Collapsed;
                }
                else
                {
                    icAlerts.Visibility = Visibility.Visible;
                    icAlerts.ItemsSource = resultTable.Rows;

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
            ResultRow row = (ResultRow)((Button)sender).DataContext;

            AlertCurrentState state = (AlertCurrentState)row[0];

            var eo = new ExploreOptions(typeof(AlertDN))
            {
                ShowFilters = false,
                SearchOnLoad = true,
                FilterOptions = 
                { 
                    GetFilter(),
                    new FilterOption("Entity.CurrentState", state)
                },
                Closed = (o, ea) => ReloadAlerts(),
            };

            if (eo.FilterOptions.First().Path == "Target")
            {
                eo.ColumnOptions = new List<ColumnOption> { new ColumnOption("Target") };
                eo.ColumnOptionsMode = ColumnOptionsMode.Remove;
            }

            Navigator.Explore(eo);
        }
    }
}
