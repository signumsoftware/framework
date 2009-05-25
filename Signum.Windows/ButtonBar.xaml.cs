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
        public static readonly DependencyProperty FixedElementsProperty =
            DependencyProperty.Register("FixedElements", typeof(List<FrameworkElement>), typeof(ButtonBar), new UIPropertyMetadata(null));
        public List<FrameworkElement> FixedElements
        {
            get { return (List<FrameworkElement>)GetValue(FixedElementsProperty); }
            set { SetValue(FixedElementsProperty, value); }
        }

        public static event GetButtonBarElementDelegate GetButtonBarElement;

        public static readonly DependencyProperty MainControlProperty =
            DependencyProperty.Register("MainControl", typeof(Control), typeof(ButtonBar));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        public static readonly DependencyProperty OkVisibleProperty =
            DependencyProperty.Register("OkVisible", typeof(bool), typeof(ButtonBar), new UIPropertyMetadata(false));
        public bool OkVisible
        {
            get { return (bool)GetValue(OkVisibleProperty); }
            set { SetValue(OkVisibleProperty, value); }
        }

        public static readonly DependencyProperty CancelVisibleProperty =
            DependencyProperty.Register("CancelVisible", typeof(bool), typeof(ButtonBar), new UIPropertyMetadata(false));
        public bool CancelVisible
        {
            get { return (bool)GetValue(CancelVisibleProperty); }
            set { SetValue(CancelVisibleProperty, value); }
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

        public event RoutedEventHandler CancelClick
        {
            add { btCancel.Click += value; }
            remove { btCancel.Click -= value; }
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

        public Button CancelButton
        {
            get { return btCancel; }
        }

        public Button SaveButton
        {
            get { return btSave; }
        }

        public Button ReloadButton
        {
            get { return btReload; }
        }

        public ButtonBar()
        {
            InitializeComponent();
            FixedElements = new List<FrameworkElement>(); 
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ToolBar_DataContextChanged);
        }

        void ToolBar_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            List<FrameworkElement> elements = new List<FrameworkElement>(FixedElements);
            if (GetButtonBarElement != null)
                elements.AddRange(GetButtonBarElement.GetInvocationList()
                    .Cast<GetButtonBarElementDelegate>()
                    .Select(d => d(DataContext, MainControl))
                    .NotNull().SelectMany(d => d).ToList());

            stackPanel.Children.Clear();

            foreach (var item in elements)
            {
                stackPanel.Children.Add((UIElement)item);
            }
        }
    }

    public delegate List<FrameworkElement> GetButtonBarElementDelegate(object entity, Control mainControl);

    public interface IHaveToolBarElements
    {
        List<FrameworkElement> ToolBarElements { get; }
    }
}
