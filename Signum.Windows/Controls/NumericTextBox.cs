using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Automation;
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary> 
    /// Have fun with this as a starting point for your own textbox. 
    /// Many other very cool features you "could" to add to the textbox. 
    /// 
    /// For example : adding min and max value. Depends on your application 
    /// 
    /// This is simply a fun piece of code that show how easy it is to implement your own textboxes and at 
    /// the same time, doing something cool and Blend like. 
    /// </summary> 
    [TemplatePart(Name = "PART_Anchor", Type = typeof(UIElement))]
    public class NumericTextBox : TextBox
    {
        static NumericTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(typeof(NumericTextBox)));
            TextProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata() { DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        }

        public static readonly DependencyProperty LargeIncrementProperty =
            DependencyProperty.Register("LargeIncrement", typeof(decimal), typeof(NumericTextBox), new UIPropertyMetadata(10.0m));
        public decimal LargeIncrement
        {
            get { return (decimal)GetValue(LargeIncrementProperty); }
            set { SetValue(LargeIncrementProperty, value); }
        }

        public static readonly DependencyProperty SmallIncrementProperty =
          DependencyProperty.Register("SmallIncrement", typeof(decimal), typeof(NumericTextBox), new UIPropertyMetadata(1.0m));
        public decimal SmallIncrement
        {
            get { return (decimal)GetValue(SmallIncrementProperty); }
            set { SetValue(SmallIncrementProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(decimal?), typeof(NumericTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (s, e) => ((NumericTextBox)s).RaiseEvent(new RoutedPropertyChangedEventArgs<decimal?>((decimal?)e.OldValue, (decimal?)e.NewValue, ValueChangedEvent))));
        public decimal? Value
        {
            get { return (decimal?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty NullableNumericConverterProperty =
            DependencyProperty.Register("NullableNumericConverter", typeof(NullableNumericConverter), typeof(NumericTextBox), new UIPropertyMetadata(NullableNumericConverter.Number, (s, e) => ((NumericTextBox)s).ConverterChanged(e.NewValue)));
        public NullableNumericConverter NullableNumericConverter
        {
            get { return (NullableNumericConverter)GetValue(NullableNumericConverterProperty); }
            set { SetValue(NullableNumericConverterProperty, value); }
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<decimal?>), typeof(NumericTextBox));
        public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private void ConverterChanged(object converter)
        {
            AutomationProperties.SetItemStatus(this, ((NullableNumericConverter)converter)?.Format);
        }

        public NumericTextBox()
        {
            Loaded += new RoutedEventHandler(NumericTextBox_Loaded);
        }

        void NumericTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            Binding b = new Binding
            {
                Source = this,
                Converter = NullableNumericConverter,
                Path = new PropertyPath(ValueProperty),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true,
                NotifyOnValidationError = true,
            };
            b.ValidationRules.Add(NullableNumericConverter);

            BindingOperations.SetBinding(this, TextProperty, b);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsReadOnly && IsEnabled && Value.HasValue && (e.Key == Key.Up || e.Key == Key.Down))
            {
                if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0)
                {
                    if (e.Key == Key.Up)
                        Value += LargeIncrement;
                    else
                        Value -= LargeIncrement;
                }
                else
                {
                    if (e.Key == Key.Up)
                        Value += SmallIncrement;
                    else
                        Value -= SmallIncrement;
                }

                e.Handled = true;
            }
            else
                base.OnKeyDown(e);
        }
    }
}
