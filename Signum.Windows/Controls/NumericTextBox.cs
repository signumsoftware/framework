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
        private MouseIncrementor mouseIncrementor;
      
        static NumericTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(typeof(NumericTextBox)));
            TextProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata() { DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            IsReadOnlyProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata() { PropertyChangedCallback = (d, e) => ((NumericTextBox)d).UpdateVisibility() });
        }

        public static readonly DependencyProperty XIncrementProperty =
            DependencyProperty.Register("XIncrement", typeof(decimal), typeof(NumericTextBox), new UIPropertyMetadata(0.1m));
        public decimal XIncrement
        {
            get { return (decimal)GetValue(XIncrementProperty); }
            set { SetValue(XIncrementProperty, value); }
        }

        public static readonly DependencyProperty YIncrementProperty =
          DependencyProperty.Register("YIncrement", typeof(decimal), typeof(NumericTextBox), new UIPropertyMetadata(1.0m));
        public decimal YIncrement
        {
            get { return (decimal)GetValue(YIncrementProperty); }
            set { SetValue(YIncrementProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(decimal?), typeof(NumericTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public decimal? Value
        {
            get { return (decimal?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty NullableDecimalConverterProperty =
            DependencyProperty.Register("NullableDecimalConverter", typeof(NullableDecimalConverter), typeof(NumericTextBox), new UIPropertyMetadata(NullableDecimalConverter.Number));
        public NullableDecimalConverter NullableDecimalConverter
        {
            get { return (NullableDecimalConverter)GetValue(NullableDecimalConverterProperty); }
            set { SetValue(NullableDecimalConverterProperty, value); }
        }

        public static readonly DependencyProperty ShowAnchorProperty =
            DependencyProperty.Register("ShowAnchor", typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(true, (d,e)=>((NumericTextBox)d).UpdateVisibility()));
        public bool ShowAnchor
        {
            get { return (bool)GetValue(ShowAnchorProperty); }
            set { SetValue(ShowAnchorProperty, value); }
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
                Converter = NullableDecimalConverter,
                Path = new PropertyPath(ValueProperty),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true,
                NotifyOnValidationError = true,
            };
            b.ValidationRules.Add(NullableDecimalConverter);

            BindingOperations.SetBinding(this, TextProperty, b);
        }

        private void UpdateVisibility()
        {
            if (anchor != null)
                anchor.Visibility = (IsReadOnly || !ShowAnchor) ? Visibility.Collapsed : Visibility.Visible;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsReadOnly && IsEnabled && Value.HasValue && (e.Key == Key.Up || e.Key == Key.Down))
            {
                if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0)
                {
                    if (e.Key == Key.Up)
                        Value += XIncrement;
                    else
                        Value -= XIncrement;
                }
                else
                {
                    if (e.Key == Key.Up)
                        Value += YIncrement;
                    else
                        Value -= YIncrement;
                }

                e.Handled = true;
            }
            else
                base.OnKeyDown(e);
        }

        UIElement anchor;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (anchor != null)
            {
                anchor.MouseDown -= new MouseButtonEventHandler(anchor_MouseDown);
                anchor.MouseMove -= new MouseEventHandler(anchor_MouseMove);
                anchor.MouseUp -= new MouseButtonEventHandler(anchor_MouseUp);
            }

            anchor = GetTemplateChild("PART_Anchor") as UIElement;

            if (anchor != null)
            {
                anchor.MouseDown += new MouseButtonEventHandler(anchor_MouseDown);
                anchor.MouseMove += new MouseEventHandler(anchor_MouseMove);
                anchor.MouseUp += new MouseButtonEventHandler(anchor_MouseUp);
            }

            UpdateVisibility();
        }

         void anchor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsReadOnly && IsEnabled && Value.HasValue)
            {
                anchor.CaptureMouse();
                mouseIncrementor = new MouseIncrementor
                {
                    Point = e.GetPosition(this),
                    Value = Value.Value
                };
            }
        }

        void anchor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            anchor.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
            mouseIncrementor = null;
        }

        void anchor_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseIncrementor == null)
                return;

            if (XIncrement == 0 && YIncrement == 0)
                return;

            if (Value == null)
            {
                //since we can't parse the value, we are out of here, i.e. user put text in our number box 
                mouseIncrementor = null;
                return; // TODO: might not be correct. Was : Exit Sub 
            }

            double intDeltaX = mouseIncrementor.Point.X - e.GetPosition(this).X;
            double intDeltaY = mouseIncrementor.Point.Y - e.GetPosition(this).Y;

            if (Math.Abs(intDeltaX) > Math.Abs(intDeltaY))
            {
                Mouse.OverrideCursor = Cursors.SizeWE;
                Value = mouseIncrementor.Value - ((int)intDeltaX / 5) * XIncrement;
            }
            else
            {
                Mouse.OverrideCursor = Cursors.SizeNS;
                Value = mouseIncrementor.Value +((int)intDeltaY / 5) * YIncrement; 
            }
        }

        protected override void OnGotFocus(System.Windows.RoutedEventArgs e)
        {
            mouseIncrementor = null;
            base.OnGotFocus(e);
        }


        protected override void OnLostFocus(System.Windows.RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            mouseIncrementor = null;
        }


        class MouseIncrementor
        {
            public decimal Value;
            public Point Point;
        }
    } 
}
