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
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Automation;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for LinksWidget.xaml
    /// </summary>
    public partial class LinksWidget : UserControl, IWidget
    {
        public decimal Order { get; set; }

        public Control Control { get; set; }
        public event Action ForceShow;

        public LinksWidget()
        {
            InitializeComponent();

            this.AddHandler(Button.ClickEvent, new RoutedEventHandler(QuickLink_MouseDown));
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(LinksWidget_DataContextChanged);
        }

        void LinksWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IdentifiableEntity ident = e.NewValue as IdentifiableEntity;

            ObservableCollection<QuickLink> links = ident != null && !ident.IsNew ? LinksClient.GetForEntity(ident.ToLiteFat(), Control) : new ObservableCollection<QuickLink>();

            lvQuickLinks.ItemsSource = links;

            if (links.IsNullOrEmpty())
                Visibility = Visibility.Collapsed;
            else
            {
                Visibility = Visibility.Visible;
                if (ForceShow != null && links.Any(a => !a.IsShy))
                    ForceShow();
            }
        }

        private void QuickLink_MouseDown(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button) //Not to capture the mouseDown of the scrollbar buttons
            {
                Button b = (Button)e.OriginalSource;
                ((QuickLink)b.DataContext).Execute();
            }
        }
    }
}
