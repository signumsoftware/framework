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

        public static Func<IdentifiableEntity, IAlert> CreateAlert { get; set; }
        public static Func<IdentifiableEntity, List<Lazy<IAlert>>> RetrieveAlerts { get; set; }


        public AlertsWidget()
        {
            InitializeComponent();

            lvAlerts.AddHandler(Button.ClickEvent, new RoutedEventHandler(Alert_MouseDown));
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
                Lazy<IAlert> alert = (Lazy<IAlert>)b.Tag;
                ViewAlert(Server.RetrieveAndForget(alert));
            }
        }

        private void btnNewAlert_Click(object sender, RoutedEventArgs e)
        {
            if (CreateAlert == null)
                throw new ApplicationException("AlertsWidget.CreateAlert is null");

            if (DataContext == null)
                return;

            IAlert alert = CreateAlert((IdentifiableEntity)DataContext);

            ViewAlert(alert);
        }

        private void ViewAlert(IAlert alert)
        {
            IAlert result = (IAlert)Navigator.View(new ViewOptions
            {
                Buttons = ViewButtons.Save,
                Closed = (o, e) => ReloadAlerts(),
            }, alert);
        }

        private void ReloadAlerts()
        {
            if (CreateAlert == null)
                throw new ApplicationException("AlertsWidget.RetrieveAlerts is null");

            IdentifiableEntity entity = DataContext as IdentifiableEntity;
            if (entity == null || entity.IsNew)
            {
                lvAlerts.ItemsSource = null;
                return;
            }

            List<Lazy<IAlert>> alerts = RetrieveAlerts((IdentifiableEntity)DataContext);

            if (alerts != null)
            {
                tbAlerts.FontWeight = alerts.Count == 0 ? FontWeights.Normal : FontWeights.Bold;

                if (alerts.Count > 0 && ForceShow != null)
                    ForceShow();
            }

            lvAlerts.ItemsSource = alerts;
        }


    }
}
