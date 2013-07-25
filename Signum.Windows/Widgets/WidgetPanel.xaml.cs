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
using System.Collections;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Collections.ObjectModel;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for ListaNavegacion.xaml
    /// </summary>
    public partial class WidgetPanel : UserControl
    {
        public static event GetWidgetDelegate GetWidgets; 

        public static readonly DependencyProperty MainControlProperty = DependencyProperty.Register("MainControl", typeof(Control), typeof(WidgetPanel));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        public static readonly RoutedEvent ExpandedCollapsedEvent = EventManager.RegisterRoutedEvent("ExpandedCollapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WidgetPanel));
        public event RoutedEventHandler ExpandedCollapsed
        {
            add { base.AddHandler(ExpandedCollapsedEvent, value); }
            remove { base.RemoveHandler(ExpandedCollapsedEvent, value); }
        }
        
        public WidgetPanel()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(LeftNavigationPanel_DataContextChanged);  
        }

        void LeftNavigationPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            List<IWidget> widgets = GetWidgets == null || e.NewValue == null ? new List<IWidget>() :
                GetWidgets.GetInvocationList().Cast<GetWidgetDelegate>().Select(d => d((ModifiableEntity)DataContext, MainControl)).NotNull().ToList();

            this.Visibility = widgets.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            stackPanel.Children.Clear();

            foreach (var item in widgets)
            {
                stackPanel.Children.Add((UIElement)item);
                item.ForceShow += () => expander.IsExpanded = true;
            }
        }

        private void expander_Expanded(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ExpandedCollapsedEvent));
        }

        private void expander_Collapsed(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ExpandedCollapsedEvent));
        }
    }

    public delegate IWidget GetWidgetDelegate(ModifiableEntity entity, Control mainControl); 

    public interface IWidget
    {
        event Action ForceShow; 
    }
}
