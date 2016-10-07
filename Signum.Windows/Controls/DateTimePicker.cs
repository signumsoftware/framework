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
using Signum.Windows;
using System.Windows.Automation.Peers;
using System.Collections.Generic;


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
    public class DateTimePicker : Control
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

        }

        internal TextBox textBox;
        internal System.Windows.Controls.Calendar monthCalendar;
        internal PickerBase pickerBase;

        public override void OnApplyTemplate()
        {
            if (textBox != null)
                BindingOperations.ClearBinding(textBox, TextBox.TextProperty);

            textBox = (TextBox)GetTemplateChild("PART_EditableTextBox");

            if (textBox != null)
                BindingOperations.SetBinding(textBox, TextBox.TextProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(SelectedDateProperty),
                    Converter = DateTimeConverter,
                    Mode = BindingMode.TwoWay,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true,
                    NotifyOnValidationError = true,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                }.Do(b => b.ValidationRules.Add(DateTimeConverter)));


            if (pickerBase != null)
                pickerBase.DropDownOpened -= pickerBase_DropDownOpened;

            pickerBase = (PickerBase)GetTemplateChild("PART_PickerBase");

            if (pickerBase != null)
                pickerBase.DropDownOpened += pickerBase_DropDownOpened;



            if (monthCalendar != null)
            {
                monthCalendar.SelectedDatesChanged -= monthCalendar_SelectedDatesChanged;
                BindingOperations.ClearBinding(monthCalendar, System.Windows.Controls.Calendar.SelectedDateProperty);
            }

            monthCalendar = (System.Windows.Controls.Calendar)GetTemplateChild("PART_DatePickerCalendar");

            if (monthCalendar != null)
            {
                monthCalendar.SelectedDatesChanged += monthCalendar_SelectedDatesChanged;
                BindingOperations.SetBinding(monthCalendar, System.Windows.Controls.Calendar.SelectedDateProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(SelectedDateProperty),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
            }

            UpdateVisibility();
        }

        bool avoidClose = false;

        void pickerBase_DropDownOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                avoidClose = true;

                BindingExpression b = textBox.GetBindingExpression(TextBox.TextProperty);
                b.UpdateSource();

                if (monthCalendar.SelectedDate.HasValue)
                    monthCalendar.DisplayDate = monthCalendar.SelectedDate.Value;
            }
            finally
            {
                avoidClose = false;
            }
        }


        void monthCalendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            BindingExpression b = textBox.GetBindingExpression(TextBox.TextProperty);
            b.UpdateSource();
            if (!avoidClose)
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

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DateTimePickerAutomationPeer(this);
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



    class DateTimePickerAutomationPeer : FrameworkElementAutomationPeer
    {
        public DateTimePickerAutomationPeer(DateTimePicker dateTimePicker)
            : base(dateTimePicker)
        {
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> childrenCore = new List<AutomationPeer>();
            DateTimePicker dtp = (DateTimePicker)base.Owner;

            AutomationPeer tb = UIElementAutomationPeer.CreatePeerForElement(dtp.textBox);
            if (tb != null)
                childrenCore.Add(tb);

            AutomationPeer button = UIElementAutomationPeer.CreatePeerForElement(dtp.pickerBase.toggleButton);
            if (button != null)
                childrenCore.Add(button);

            if (dtp.pickerBase.IsDropDownOpen && dtp.monthCalendar != null)
            {
                AutomationPeer calendar = UIElementAutomationPeer.CreatePeerForElement(dtp.monthCalendar);
                if (calendar != null)
                    childrenCore.Add(calendar);
            }

            return childrenCore;
        }

        protected override string GetClassNameCore()
        {
            return typeof(DateTimePicker).Name;
        }

        protected override string GetItemStatusCore()
        {
            DateTimePicker dtp = (DateTimePicker)base.Owner;
            return dtp.DateTimeConverter.Format;
        }
    }
}