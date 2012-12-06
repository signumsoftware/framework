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
    /// Interaction logic for ButtonBar.xaml
    /// </summary>
    public partial class ButtonBar : UserControl
    {
        public static readonly DependencyProperty OkVisibleProperty =
             DependencyProperty.Register("OkVisible", typeof(bool), typeof(ButtonBar), new UIPropertyMetadata(false));
        public bool OkVisible
        {
            get { return (bool)GetValue(OkVisibleProperty); }
            set { SetValue(OkVisibleProperty, value); }
        }

        public static readonly DependencyProperty SaveVisibleProperty =
          DependencyProperty.Register("SaveVisible", typeof(bool), typeof(ButtonBar), new UIPropertyMetadata(false));
        public bool SaveVisible
        {
            get { return (bool)GetValue(SaveVisibleProperty); }
            set { SetValue(SaveVisibleProperty, value); }
        }

        public static readonly DependencyProperty ReloadVisibleProperty =
            DependencyProperty.Register("ReloadVisible", typeof(bool), typeof(ButtonBar), new UIPropertyMetadata(false));
        public bool ReloadVisible
        {
            get { return (bool)GetValue(ReloadVisibleProperty); }
            set { SetValue(ReloadVisibleProperty, value); }
        }
   
        public event RoutedEventHandler OkClick
        {
            add { btOk.Click += value; }
            remove { btOk.Click -= value; }
        }

        public event RoutedEventHandler SaveClick
        {
            add { btSave.Click += value; }
            remove { btSave.Click -= value; }
        }

        public event RoutedEventHandler ReloadClick
        {
            add { btReload.Click += value; }
            remove { btReload.Click -= value; }
        }

        public Button OkButton
        {
            get { return btOk; }
        }

        public Button ReloadButton
        {
            get { return btReload; }
        }

        public ViewMode ViewMode { get; set; }

        public ButtonBar()
        {
            InitializeComponent();
        }

        public void SetButtons(List<FrameworkElement> elements)
        {
            wrapPanel.Children.RemoveRange(2, wrapPanel.Children.Count - 3);
            for (int i = 0; i < elements.Count; i++)
            {
                wrapPanel.Children.Insert(i + 2, elements[i]);
            }
        }
    }

    public delegate List<FrameworkElement> GetButtonBarElementDelegate(object entity, ButtonBarEventArgs context);

    public interface IHaveToolBarElements
    {
        List<FrameworkElement> GetToolBarElements(object dataContext, ButtonBarEventArgs ctx);
    }

    public class ButtonBarEventArgs
    {
        public Control MainControl { get; set; }
        public ViewMode ViewButtons { get; set; }
        public bool SaveProtected { get; set; }
    }

    public enum ViewMode
    {
        View,
        Navigate
    }
}
