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

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for LinksWidget.xaml
    /// </summary>
    public partial class LinksWidget : UserControl, IWidget
    {
        public static event Func<object, Control, QuickLink> GetLinks; 
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
            List<QuickLink> links = new List<QuickLink>();

            if(Control is IHaveQuickLinks)
                links.AddRange(((IHaveQuickLinks)Control).QuickLinks());

            if (GetLinks != null)
                links.AddRange(GetLinks.GetInvocationList().Cast<Func<object, Control, QuickLink>>().Select(a => a(DataContext, Control)).NotNull());

            lvQuickLinks.ItemsSource = links;

            if (links.Count == 0)
                Visibility = Visibility.Collapsed;
            else
            {
                Visibility = Visibility.Visible;
                if (ForceShow != null)
                    ForceShow(); 
            }
        }

        private void QuickLink_MouseDown(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button) //Not to capture the mouseDown of the scrollbar buttons
            {
                Button b = (Button)e.OriginalSource;
                ((Action)b.Tag).Invoke();
            }
        }
    }

    /// <summary>
    /// Controls must implement this interface to have the left navigation panel
    /// </summary>
    public interface IHaveQuickLinks
    {
        List<QuickLink> QuickLinks();
    }

    /// <summary>
    /// Represents an item of the left navigation panel
    /// </summary>
    public class QuickLink
    {
        public QuickLink(string label)
        {
            this.Label = label;
        }

        /// <summary>
        /// Display name of the item
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Action to be executed on the mouseDoubleClick of the item
        /// </summary>
        public Action Action { get; set; }
    }
}
