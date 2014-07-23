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
    /// Interaction logic for SelectorWindow.xaml
    /// </summary>
    public partial class SelectorWindow : Window
    {
        public class ElementInfo
        {
            public object Element { get; set; }
            public ImageSource Image { get; set; }
            public string Text { get; set; }
        }

        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Elements", typeof(ElementInfo[]), typeof(SelectorWindow), new UIPropertyMetadata(null));
        public ElementInfo[] Elements
        {
            get { return (ElementInfo[])GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }

        public static readonly DependencyProperty SelectedElementProperty =
            DependencyProperty.Register("SelectedElement", typeof(object), typeof(SelectorWindow), new UIPropertyMetadata(null));
        public object SelectedElement
        {
            get { return (object)GetValue(SelectedElementProperty); }
            set { SetValue(SelectedElementProperty, value); }
        }


        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(SelectorWindow), new UIPropertyMetadata(null));
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }


        public static bool ShowDialog<T>(IEnumerable<T> elements, out T selectedElement,
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

            selectedElement = elements.Only();
            if (selectedElement != null)
                return true;

            if (elementIcon == null)
                elementIcon = o => null;

            if (elementText == null)
                elementText = o => o.ToString();

            SelectorWindow w = new SelectorWindow()
            {
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                Owner = owner,
                Title = title,
                Message = message,
                Elements = elements.Select(e => new ElementInfo()
                {
                    Element = e,
                    Image = elementIcon(e),
                    Text = elementText(e)
                }).ToArray()
            };
            bool res = w.ShowDialog() ?? false;
            if (res)
                selectedElement = (T)w.SelectedElement;
            else
                selectedElement = default(T);
            return res;
        }

        public SelectorWindow()
        {
            InitializeComponent();

			AutomationProperties.SetName(this, "SelectorWindow");
            
			this.Message = SearchMessage.SelectAnElement.NiceToString();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SelectedElement = ((ElementInfo)((ToggleButton)sender).DataContext).Element;
            DialogResult = true;
            Close();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SelectorWindowAutomationPeer(this);
        }
    }

    public class SelectorWindowAutomationPeer : WindowAutomationPeer
    {
        public SelectorWindowAutomationPeer(SelectorWindow selectorWindow) : base(selectorWindow)
        {
        }

        protected override string GetClassNameCore()
        {
            return "SelectorWindow";
        }
    }
}
