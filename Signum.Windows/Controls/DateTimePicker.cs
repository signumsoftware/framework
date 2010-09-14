//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;       // Debug
using System.Globalization;     // CultureInfo
using System.Windows;
using System.Windows.Controls;  // Control
using System.Windows.Controls.Primitives; //ButtonBase
using System.Windows.Data;      // IValueConverter
using System.Windows.Input;
using System.Windows.Media;     
using System.Windows.Threading; // DispatcherPriority
using Signum.Utilities; 
using System.Windows.Controls;
using Signum.Windows;


namespace Signum.Windows
{
    /// <summary>
    /// The DatePicker control allows the user to enter or select a date and display it in 
    /// the specified format. User can limit the date that can be selected by setting the 
    /// selection range.  You might consider using a DatePicker control instead of a MonthCalendar 
    /// if you need custom date formatting and limit the selection to just one date.
    /// </summary>
    [TemplatePart(Name = "PART_EditableTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_DatePickerCalendar", Type = typeof(System.Windows.Controls.Calendar))]
    public class DateTimePicker: Control
    {
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(DateTimePicker), new FrameworkPropertyMetadata(true, (d, e) => ((DateTimePicker)d).UpdateVisibility()));
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(DateTimePicker), new FrameworkPropertyMetadata(false, (d, e) => ((DateTimePicker)d).UpdateVisibility()));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }


        public static readonly DependencyProperty DateTimeConverterProperty =
          DependencyProperty.Register("DateTimeConverter", typeof(DateTimeConverter), typeof(DateTimePicker), new UIPropertyMetadata(DateTimeConverter.DateAndTime));
        public DateTimeConverter DateTimeConverter
        {
            get { return (DateTimeConverter)GetValue(DateTimeConverterProperty); }
            set { SetValue(DateTimeConverterProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(DateTimePicker), new UIPropertyMetadata(null));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }


        public static readonly DependencyProperty SelectedDateProperty =
         DependencyProperty.Register("SelectedDate", typeof(DateTime?), typeof(DateTimePicker), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public DateTime? SelectedDate
        {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        static DateTimePicker()
        {
            IsEnabledProperty.OverrideMetadata(typeof(DateTimePicker), new FrameworkPropertyMetadata() { PropertyChangedCallback = new PropertyChangedCallback(UpdateVisibility) });
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimePicker), new FrameworkPropertyMetadata(typeof(DateTimePicker)));
        }

        public DateTimePicker()
        {
            this.Loaded += new RoutedEventHandler(DateTimePicker_Loaded);
        }

        void DateTimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= DateTimePicker_Loaded;
            DateTimeConverter dtc = DateTimeConverter; 
            Binding b = new Binding
            {
                Source = this,
                Path = new PropertyPath(SelectedDateProperty),
                Converter = dtc,
                Mode = BindingMode.TwoWay,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions= true,
                NotifyOnValidationError = true,
            };
            b.ValidationRules.Add(dtc);
            //PresentationTraceSources.SetTraceLevel(b, PresentationTraceLevel.High);

            BindingOperations.SetBinding(this, TextProperty, b);
        }

        TextBox textBox;
        System.Windows.Controls.Calendar monthCalendar;
        PickerBase pickerBase;

        public override void OnApplyTemplate()
        {
            textBox = (TextBox)GetTemplateChild("PART_EditableTextBox");
            pickerBase = (PickerBase)GetTemplateChild("PART_PickerBase");

            if (monthCalendar != null)
                monthCalendar.SelectedDatesChanged -= monthCalendar_SelectedDatesChanged;
            monthCalendar = (System.Windows.Controls.Calendar)GetTemplateChild("PART_DatePickerCalendar");
            if (monthCalendar != null)
                monthCalendar.SelectedDatesChanged += monthCalendar_SelectedDatesChanged;


            UpdateVisibility();
        }


        void monthCalendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            pickerBase.IsDropDownOpen = false;
        }

        static void UpdateVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)d;
            dtp.UpdateVisibility();
        }

        protected void UpdateVisibility()
        {
            if (textBox != null && monthCalendar != null)
            {
                textBox.IsEnabled = IsEnabled;
                textBox.IsReadOnly = !IsEditable || IsReadOnly;
                monthCalendar.IsEnabled = IsEnabled && !IsReadOnly;
            }
        }

        //protected override void OnDropDownOpenChanged(DependencyPropertyChangedEventArgs e)
        //{
        //    if (((bool)e.NewValue) && this.Value.HasValue)
        //    {
        //        this.monthCalendar.VisibleMonth = this.Value.Value;
        //    }
        //    base.OnDropDownOpenChanged(e);
        //}
    }
}
