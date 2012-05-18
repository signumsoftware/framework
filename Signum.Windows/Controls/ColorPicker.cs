using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using Signum.Windows.ColorUtils;

namespace Signum.Windows
{
    [TemplatePart(Name = "PART_EditableTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ColorSelector", Type = typeof(ColorSelector))]
    public class ColorPicker : Control
    {
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(ColorPicker), 
                new FrameworkPropertyMetadata(true, (d, e) => ((ColorPicker)d).UpdateVisibility()));
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ColorPicker), 
                new FrameworkPropertyMetadata(false, (d, e) => ((ColorPicker)d).UpdateVisibility()));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPicker),
                new FrameworkPropertyMetadata(Colors.Blue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((ColorPicker)d).SelectedColorChanged()));

    
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        static ColorPicker()
        {
            IsEnabledProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata() { PropertyChangedCallback = (d, e) => ((ColorPicker)d).UpdateVisibility() });
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        TextBox textBox;
        ColorSelector colorSelector;
        PickerBase pickerBase; 

        public override void OnApplyTemplate()
        {
            if (pickerBase != null)
            {
                pickerBase.DropDownClosed -= new RoutedEventHandler(pickerBase_DropDownClosed);
                pickerBase.DropDownOpened -= new RoutedEventHandler(pickerBase_DropDownOpened);
            }
            textBox = (TextBox)GetTemplateChild("PART_EditableTextBox");
            colorSelector =  (ColorSelector)GetTemplateChild("PART_ColorSelector");
            pickerBase = (PickerBase)GetTemplateChild("PART_PickerBase");

            if (pickerBase != null)
            {
                pickerBase.DropDownClosed += new RoutedEventHandler(pickerBase_DropDownClosed);
                pickerBase.DropDownOpened += new RoutedEventHandler(pickerBase_DropDownOpened);
            }

            UpdateVisibility();
        }

        bool forceClosing;
        private void SelectedColorChanged()
        {
            if (pickerBase == null)
                return;

            if (pickerBase.IsDropDownOpen)
                forceClosing = true;
            pickerBase.IsDropDownOpen = false;
        }

        void pickerBase_DropDownOpened(object sender, RoutedEventArgs e)
        {
            colorSelector.SelectedColor = SelectedColor;
        }

        void pickerBase_DropDownClosed(object sender, RoutedEventArgs e)
        {
            if (forceClosing)
                forceClosing = false;
            else
                SelectedColor = colorSelector.SelectedColor;
        }
       
        protected void UpdateVisibility()
        {
            if (textBox != null && colorSelector != null)
            {
                textBox.IsEnabled = IsEnabled;
                textBox.IsReadOnly = !IsEditable || IsReadOnly;
                colorSelector.IsEnabled = IsEnabled && !IsReadOnly;
            }
        }

    }
}
