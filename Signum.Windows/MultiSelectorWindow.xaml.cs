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
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Signum.Utilities;
using Signum.Entities;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for MultiSelectorWindow.xaml
    /// </summary>
    public partial class MultiSelectorWindow : Window
    {
        public class ElementInfo
        {
            public object Element { get; set; }
            public ImageSource Image { get; set; }
            public string Text { get; set; }
            public bool Selected { get; set; }
        }

        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Elements", typeof(ElementInfo[]), typeof(MultiSelectorWindow), new UIPropertyMetadata(null));
        public ElementInfo[] Elements
        {
            get { return (ElementInfo[])GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }

        public static readonly DependencyProperty MultiSelectProperty =
          DependencyProperty.Register("MultiSelect", typeof(bool), typeof(MultiSelectorWindow), new PropertyMetadata(true));
        public bool MultiSelect
        {
            get { return (bool)GetValue(MultiSelectProperty); }
            set { SetValue(MultiSelectProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MultiSelectorWindow), new UIPropertyMetadata(null));
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static bool ShowDialog<T>(IEnumerable<T> elements, out IEnumerable<T> selectedElements,
           Func<T, ImageSource> elementIcon = null,
           Func<T, string> elementText = null,
           string title = null,
           string message = null,
           Window owner = null)
        {
            if (title == null)
                title = SearchMessage.SelectAnElement.NiceToString();

            if (message == null)
                message = SearchMessage.SelectAnElement.NiceToString();

            if (elementIcon == null)
                elementIcon = o => null;

            if (elementText == null)
                elementText = o => o.ToString();

            MultiSelectorWindow w = new MultiSelectorWindow()
            {
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                Owner = owner,
                Title = title,
                Message = message,
                Elements = elements.Select(e => new ElementInfo()
                {
                    Selected = false,
                    Element = e,
                    Image = elementIcon(e),
                    Text = elementText(e)
                }).ToArray()
            };
            bool res = w.ShowDialog() ?? false;
            if (res)
                selectedElements = w.SelectedItems<T>();
            else
                selectedElements = default(IEnumerable<T>);
            return res;
        }

        private IEnumerable<T> SelectedItems<T>()
        {
            return Elements.Where(e => e.Selected).Select(e => (T)e.Element).ToList();
        }

        public MultiSelectorWindow()
        {
            InitializeComponent();

            AutomationProperties.SetName(this, "MultiSelectorWindow");
            
			this.Message = SearchMessage.SelectAnElement.NiceToString();
        }

        private void btAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new MultiSelectorWindowAutomationPeer(this);
        }
    }

    public class MultiSelectorWindowAutomationPeer : WindowAutomationPeer
    {
        public MultiSelectorWindowAutomationPeer(MultiSelectorWindow selectorWindow) : base(selectorWindow)
        {
        }

        protected override string GetClassNameCore()
        {
            return "MultiSelectorWindow";
        }
    }
}
