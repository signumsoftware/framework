using Signum.Utilities;
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

namespace Signum.Windows.Controls
{
    /// <summary>
    /// Interaction logic for DateSpan.xaml
    /// </summary>
    public partial class DateSpanUI : UserControl
    {
        public DateSpan Value
        {
            get { return (DateSpan)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(DateSpan), typeof(DateSpanUI), new FrameworkPropertyMetadata(new DateSpan(0, 0, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((DateSpanUI)d).DateSpanValueChanged(e)));

        public DateSpanUI()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>UpdateValue(Value);
            txtYear.Text = DateTimeMessage._0Years.NiceToString().Formato("");
            txtMonth.Text = DateTimeMessage._0Month.NiceToString().Formato("");
            txtDay.Text = DateTimeMessage._0Days.NiceToString().Formato("");

            this.Day.ValueChanged += (s, e) => Value = new DateSpan(Value.Years, Value.Months, (int?)e.NewValue ?? 0);
            this.Month.ValueChanged += (s, e) => Value = new DateSpan(Value.Years, (int?)e.NewValue ?? 0, Value.Days);
            this.Year.ValueChanged += (s, e) => Value = new DateSpan((int?)e.NewValue ?? 0, Value.Months, Value.Days);
        }

        private void DateSpanValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
                UpdateValue((DateSpan)e.NewValue);
        }

        private void UpdateValue(DateSpan ds)
        {
            Day.Value = ds.Days;
            Month.Value = ds.Months;
            Year.Value = ds.Years;
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
          DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(DateSpanUI), new UIPropertyMetadata(false));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsEditableProperty =
          DependencyProperty.Register("IsEditable", typeof(bool), typeof(DateSpanUI), new FrameworkPropertyMetadata(true, (d, e) => ((DateSpanUI)d).UpdateVisibility()));
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        protected void UpdateVisibility()
        {
            if (Day != null)
            {
                Day.IsEnabled = IsEnabled;
                Day.IsReadOnly = !IsEditable || IsReadOnly;
            }

            if (Month != null)
            {
                Month.IsEnabled = IsEnabled;
                Month.IsReadOnly = !IsEditable || IsReadOnly;
            }
            if (Year != null)
            {
                Year.IsEnabled = IsEnabled;
                Year.IsReadOnly = !IsEditable || IsReadOnly;
            }

        }

        private void Month_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal?> e)
        {
            var newValor = e.NewValue ?? 0;
            this.Value = new DateSpan(this.Value.Years, (int)newValor, this.Value.Days);
        }

        private void Year_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal?> e)
        {
            var newValor = e.NewValue ?? 0;
            this.Value = new DateSpan((int)newValor, this.Value.Months, this.Value.Days);
        }
    }
}
