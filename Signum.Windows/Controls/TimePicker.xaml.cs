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
using Signum.Windows;
using System.Globalization;
using Signum.Utilities;
using System.Windows.Automation;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for TimePicker.xaml
    /// </summary>
    public partial class TimePicker : UserControl
    {
        public TimePicker()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(TimePicker_Loaded);
        }

        void TimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(TimePartProperty),
                Mode = BindingMode.TwoWay,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true,
                NotifyOnValidationError = true,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                Converter = TimeSpanConverter,
            }.Do(b => b.ValidationRules.Add(TimeSpanConverter)));
        }

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(TimePicker), new FrameworkPropertyMetadata(true, (d, e) => ((TimePicker)d).UpdateVisibility()));
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(TimePicker), new UIPropertyMetadata(false));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty TimeSpanConverterProperty =
            DependencyProperty.Register("TimeSpanConverter", typeof(TimeSpanConverter), typeof(TimePicker), new UIPropertyMetadata(TimeSpanConverter.Minutes, (s,e)=>((TimePicker)s).OnConverterChanged(e.NewValue)));
        public TimeSpanConverter TimeSpanConverter
        {
            get { return (TimeSpanConverter)GetValue(TimeSpanConverterProperty); }
            set { SetValue(TimeSpanConverterProperty, value); }
        }

        public static readonly DependencyProperty TimePartProperty =
            DependencyProperty.Register("TimePart", typeof(TimeSpan?), typeof(TimePicker), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((TimePicker)d).TimePartChanged(e)));
        public TimeSpan? TimePart
        {
            get { return (TimeSpan?)GetValue(TimePartProperty); }
            set { SetValue(TimePartProperty, value); }
        }

        private void OnConverterChanged(object converter)
        {
            AutomationProperties.SetItemStatus(this, ((TimeSpanConverter)converter)?.Format);
        }

        private void TimePartChanged(DependencyPropertyChangedEventArgs e)
        {
            //if (e.NewValue == null && DatePart == null)
            //    SelectedDateTime = null;
            //else
            //    if (e.NewValue != null && DatePart != null)
            //        SelectedDateTime = ((DateTime)DatePart).Add((TimeSpan)e.NewValue);
        }

        static void UpdateVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimePicker tp = (TimePicker)d;
            tp.UpdateVisibility();
        }

        protected void UpdateVisibility()
        {
            if (textBox != null)
            {
                textBox.IsEnabled = IsEnabled;
                textBox.IsReadOnly = !IsEditable || IsReadOnly;
            }
        }
    }
}
