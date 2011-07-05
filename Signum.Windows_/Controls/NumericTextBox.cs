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
            DependencyProperty.Register("Value", typeof(decimal?), typeof(NumericTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public decimal? Value
        {
            get { return (decimal?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty NullableNumericConverterProperty =
            DependencyProperty.Register("NullableNumericConverter", typeof(NullableNumericConverter), typeof(NumericTextBox), new UIPropertyMetadata(NullableNumericConverter.Number));
        public NullableNumericConverter NullableNumericConverter
        {
            get { return (NullableNumericConverter)GetValue(NullableNumericConverterProperty); }
            set { SetValue(NullableNumericConverterProperty, value); }
        }

        public static readonly DependencyProperty ShowAnchorProperty =
            DependencyProperty.Register("ShowAnchor", typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(false, (d,e)=>((NumericTextBox)d).UpdateVisibility()));
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

            if (LargeIncrement == 0 && SmallIncrement == 0)
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
                Value = mouseIncrementor.Value - ((int)intDeltaX / 5) * LargeIncrement;
            }
            else
            {
                Mouse.OverrideCursor = Cursors.SizeNS;
                Value = mouseIncrementor.Value +((int)intDeltaY / 5) * SmallIncrement; 
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
