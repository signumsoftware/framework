using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Signum.Windows
{
    [TemplatePart(Name = "PART_Button", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    public class PickerBase : Control
    {
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(PickerBase), new FrameworkPropertyMetadata(true));
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(PickerBase), new FrameworkPropertyMetadata(false));
        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register("ButtonContent", typeof(object), typeof(PickerBase), new FrameworkPropertyMetadata(null, (d, e) => ((PickerBase)d).ButtonOrPopupContentChanged(e)));
        public object ButtonContent
        {
            get { return (object)GetValue(ButtonContentProperty); }
            set { SetValue(ButtonContentProperty, value); }
        }

        public static readonly DependencyProperty PopupContentProperty =
            DependencyProperty.Register("PopupContent", typeof(object), typeof(PickerBase), new FrameworkPropertyMetadata(null, (d, e) => ((PickerBase)d).ButtonOrPopupContentChanged(e)));
        public object PopupContent
        {
            get { return (object)GetValue(PopupContentProperty); }
            set { SetValue(PopupContentProperty, value); }
        }


        public static readonly RoutedEvent DropDownClosedEvent = EventManager.RegisterRoutedEvent(
            "DropDownClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PickerBase));
        public event RoutedEventHandler DropDownClosed
        {
            add { AddHandler(DropDownClosedEvent, value); }
            remove { RemoveHandler(DropDownClosedEvent, value); }
        }

        protected virtual void OnDropDownClosed(EventArgs e)
        {
            base.RaiseEvent(new RoutedEventArgs(DropDownClosedEvent));
        }


        public static readonly RoutedEvent DropDownOpenedEvent = EventManager.RegisterRoutedEvent(
            "DropDownOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PickerBase));
        public event RoutedEventHandler DropDownOpened
        {
            add { AddHandler(DropDownOpenedEvent, value); }
            remove { RemoveHandler(DropDownOpenedEvent, value); }
        }

        protected virtual void OnDropDownOpened(EventArgs e)
        {
            base.RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent));
        }

        static PickerBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PickerBase), new FrameworkPropertyMetadata(typeof(PickerBase)));
        }

        private void ButtonOrPopupContentChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)base.RemoveLogicalChild(e.OldValue);
            if (e.NewValue != null)base.AddLogicalChild(e.NewValue);
        }

        internal ToggleButton toggleButton;
        Popup popup; 
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (this.popup != null)
            {
                popup.Closed -= new EventHandler(OnPopupClosed);
                popup.Opened -= new EventHandler(OnPopupOpened);
            }

            toggleButton = (ToggleButton)base.GetTemplateChild("PART_Button");
            popup = (Popup)base.GetTemplateChild("PART_Popup");

            if (this.popup != null)
            {
                popup.Closed += new EventHandler(OnPopupClosed);
                popup.Opened += new EventHandler(OnPopupOpened);
            }
        }

        void OnPopupOpened(object sender, EventArgs e)
        {
            Mouse.Capture(this, CaptureMode.SubTree);

            this.OnDropDownOpened(EventArgs.Empty);
        }

        void OnPopupClosed(object source, EventArgs e)
        {
            if (Mouse.Captured == this)
            {
                Mouse.Capture(this, CaptureMode.None);
            }

            this.OnDropDownClosed(EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (Mouse.Captured == this && e.OriginalSource == this)
            {
                popup.IsOpen = false;
            }

            base.OnMouseDown(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            DependencyObject obj = e.NewFocus as DependencyObject;

            obj = obj?.VisualParents().FirstOrDefault(a => a is Visual || a is Visual3D);

            if (obj == null || !popup.Child.IsAncestorOf(obj))
                popup.IsOpen = false;

            base.OnLostKeyboardFocus(e);
        }

        private void KeyDownHandler(KeyEventArgs e)
        {
            bool flag = false;
            Key systemKey = e.Key;
            if (systemKey == Key.System)
            {
                systemKey = e.SystemKey;
            }
            switch (systemKey)
            {
                case Key.F4:
                    if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.None)
                    {
                        IsDropDownOpen = true;
                        flag = true;
                    }
                    break;    

                case Key.Escape:
                    if (this.IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        flag = true;
                    }
                    break;         

                case Key.Return:
                    if (this.IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        flag = true;
                    }
                    break;

                default:
                    flag = false; break;
            }
         
            if (flag)
            {
                e.Handled = true;
            }
        }
    }
}
