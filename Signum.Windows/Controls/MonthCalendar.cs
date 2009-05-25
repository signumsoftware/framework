//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections;              // IEnumerable
using System.Collections.Generic;      // List<T>
using System.Collections.ObjectModel;  // ReadOnlyCollection<T>
using System.Collections.Specialized;  // NotifyCollectionChangedEventArgs
using System.Diagnostics;       // Debug
using System.Globalization;     // CultureInfo
using System.Windows;
using System.Windows.Controls;  // Control
using System.Windows.Controls.Primitives;
using System.Windows.Data;      // IValueConverter
using System.Windows.Input;     // RoutedCommand
using System.Windows.Media;     
using System.Windows.Threading; // DispatcherPriority
using System.Xml;               // XmlAttribute
using System.Linq;
using Signum.Utilities;
using Signum.Windows.DateUtils;


namespace Signum.Windows
{
    /// <summary>
    /// The month calendar control implements a calendar-like user interface,
    /// that provides the user with a very intuitive and recognizable method
    /// of selecting a date, a contiguous or discrete ranges of dates using
    /// a visual display. Users can customize the look of the calendar portion
    /// of the control by setting titles, dates, fonts and backgrounds.
    /// </summary>
    [TemplatePart(Name = "PART_VisibleDaysHost", Type = typeof(MonthCalendarContainer))]
    [TemplatePart(Name = "PART_PreviousButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_NextButton", Type = typeof(ButtonBase))]
    public class MonthCalendar : Control
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static Constructor
        /// </summary>
        static MonthCalendar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MonthCalendar), new FrameworkPropertyMetadata(typeof(MonthCalendar)));

            _gotoCommand = new RoutedCommand("Goto", typeof(MonthCalendar));
            CommandManager.RegisterClassCommandBinding(typeof(MonthCalendar), new CommandBinding(MonthCalendar.GotoCommand, new ExecutedRoutedEventHandler(OnExecuteGotoCommand), new CanExecuteRoutedEventHandler(OnQueryGotoCommand)));

            _nextCommand = new RoutedCommand("Next", typeof(MonthCalendar));
            CommandManager.RegisterClassCommandBinding(typeof(MonthCalendar), new CommandBinding(MonthCalendar.NextCommand, new ExecutedRoutedEventHandler(OnExecuteNextCommand), new CanExecuteRoutedEventHandler(OnQueryNextCommand)));
            CommandManager.RegisterClassInputBinding(typeof(MonthCalendar), new InputBinding(MonthCalendar.NextCommand, new KeyGesture(Key.PageDown)));

            _previousCommand = new RoutedCommand("Previous", typeof(MonthCalendar));
            CommandManager.RegisterClassCommandBinding(typeof(MonthCalendar), new CommandBinding(MonthCalendar.PreviousCommand, new ExecutedRoutedEventHandler(OnExecutePreviousCommand), new CanExecuteRoutedEventHandler(OnQueryPreviousCommand)));
            CommandManager.RegisterClassInputBinding(typeof(MonthCalendar), new InputBinding(MonthCalendar.PreviousCommand, new KeyGesture(Key.PageUp)));

            IsTabStopProperty.OverrideMetadata(typeof(MonthCalendar), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(MonthCalendar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(MonthCalendar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public MonthCalendar()
            : base()
        {}

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns a string representation for this control.
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " VisibleMonth: " + VisibleMonth.ToShortDateString() + ", SelectedDate: " + SelectedDate.ToString();
        }

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_mccContainer != null)
            {
                _mccContainer.SelectionChanged -= new SelectionChangedEventHandler(OnContainerSelectionChanged);
                _mccContainer = null;
            }

            // Walk the visual tree to find the MonthCalendarContainer.
            _mccContainer = GetTemplateChild(c_VisibleDaysHostTemplateName) as MonthCalendarContainer;

            if (_mccContainer != null)
            {
                _mccContainer.ItemsSource = VisibleDays;

                RefreshDayTemplate();

                _mccContainer.SelectionChanged += new SelectionChangedEventHandler(OnContainerSelectionChanged);
            }

            RefreshPreviousButtonStyle();
            RefreshNextButtonStyle();
        }

        /// <summary>
        /// Return the UI element corresponding to the given date.
        /// Returns null if the date does not belong to the visible days
        /// or if no UI has been generated for it.
        /// </summary>
        public MonthCalendarItem GetContainerFromDate(DateTime date)
        {
            CalendarDate cdate = GetCalendarDateByDate(date);
            if (cdate != null && _mccContainer != null)
            {
                return _mccContainer.ItemContainerGenerator.ContainerFromItem(cdate) as MonthCalendarItem;
            }
            return null;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events


        public static readonly RoutedEvent SelectedDateChangedEvent = EventManager.RegisterRoutedEvent(
            "SelectedDateChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<DateTime?>), typeof(MonthCalendar));
        public event RoutedPropertyChangedEventHandler<DateTime?> SelectedDateChanged
        {
            add { AddHandler(SelectedDateChangedEvent, value); }
            remove { RemoveHandler(SelectedDateChangedEvent, value); }
        }


        /// <summary>
        /// An event fired when the display month switches
        /// </summary>
        public static readonly RoutedEvent VisibleMonthChangedEvent = EventManager.RegisterRoutedEvent(
            "VisibleMonthChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<DateTime>), typeof(MonthCalendar));

        /// <summary>
        /// Add / Remove VisibleMonthChangedEvent handler
        /// </summary>
        public event RoutedPropertyChangedEventHandler<DateTime> VisibleMonthChanged
        {
            add { AddHandler(VisibleMonthChangedEvent, value); }
            remove { RemoveHandler(VisibleMonthChangedEvent, value); }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Commands
        //
        //-------------------------------------------------------------------

        #region Public Commands

        private static RoutedCommand _gotoCommand = null;

        /// <summary>
        /// Go to month
        /// </summary>
        /// <remarks>
        /// if the argument is null,     GotoCommand will switch to DateTime.Now
        /// if the argument is DateTime, GotoCommand will switch to the month from the argument
        /// </remarks>
        public static RoutedCommand GotoCommand
        {
            get { return _gotoCommand; }
        }

        private static void OnQueryGotoCommand(object target, CanExecuteRoutedEventArgs args)
        {
            MonthCalendar mcc = (MonthCalendar)target;

            int offset = 0;

            if (args.Parameter == null)
            {
                offset = MonthCalendarHelper.SubtractByMonth(DateTime.Now, mcc.VisibleMonth);     
            }
            else if (args.Parameter is DateTime)
            {
                offset = MonthCalendarHelper.SubtractByMonth((DateTime)args.Parameter, mcc.VisibleMonth);
            }

            if (offset != 0)
            {
                DateTime newValue = mcc.VisibleMonth.AddMonths(offset);

                args.CanExecute = (MonthCalendarHelper.CompareYearMonth(newValue, mcc.VisibleMonth) != 0
                                   && MonthCalendarHelper.IsWithinRange(newValue, mcc.MinDate, mcc.MaxDate));
            }
        }

        private static void OnExecuteGotoCommand(object target, ExecutedRoutedEventArgs args)
        {
            MonthCalendar mcc = (MonthCalendar)target;

            int offset = 0;
            if (args.Parameter == null)
            {
                offset = MonthCalendarHelper.SubtractByMonth(DateTime.Now, mcc.VisibleMonth);
            }
            else if (args.Parameter is DateTime)
            {
                offset = MonthCalendarHelper.SubtractByMonth((DateTime)args.Parameter, mcc.VisibleMonth);
            }

            Debug.Assert(offset != 0);
            if (offset > 0)
            {
                mcc.ScrollVisibleMonth(1, offset);
            }
            else
            {
                mcc.ScrollVisibleMonth(-1, Math.Abs(offset));
            }
        }


        private static RoutedCommand _nextCommand = null;
        private static RoutedCommand _previousCommand = null;

        /// <summary>
        /// Switch to next month
        /// </summary>
        public static RoutedCommand NextCommand
        {
            get { return _nextCommand; }
        }

        /// <summary>
        /// Switch to previous month
        /// </summary>
        public static RoutedCommand PreviousCommand
        {
            get { return _previousCommand; }
        }

        private static void OnQueryNextCommand(object target, CanExecuteRoutedEventArgs args)
        {
            MonthCalendar mcc = (MonthCalendar)target;

            args.CanExecute = MonthCalendarHelper.CompareYearMonth(mcc.VisibleMonth, mcc.MaxDate) < 0;
        }

        private static void OnExecuteNextCommand(object target, ExecutedRoutedEventArgs args)
        {
            MonthCalendar mcc = (MonthCalendar)target;
            mcc.ScrollVisibleMonth(1, 0);
        }

        private static void OnQueryPreviousCommand(object target, CanExecuteRoutedEventArgs args)
        {
            MonthCalendar mcc = (MonthCalendar)target;
            args.CanExecute = (MonthCalendarHelper.CompareYearMonth(mcc.VisibleMonth, mcc.MinDate) > 0);
        }

        private static void OnExecutePreviousCommand(object target, ExecutedRoutedEventArgs args)
        {
            MonthCalendar mcc = (MonthCalendar)target;
            mcc.ScrollVisibleMonth(-1, 0);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        #region SelectedDates/Date 

        /// <summary>
        /// The DependencyProperty for SelectedDate property
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty =
                DependencyProperty.Register(
                        "SelectedDate",
                        typeof(DateTime?),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (DateTime?)null,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnSelectedDateChanged)),
                        new ValidateValueCallback(IsValidNullableDate));

        /// <summary>
        /// The first date in the current selection or returns null if the selection is empty
        /// </summary>
        public DateTime? SelectedDate
        {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthCalendar mc = (MonthCalendar)d;

            DateTime? oldV = (DateTime?)e.OldValue;
            DateTime? newV = (DateTime?)e.NewValue;

            if (oldV != newV)
            {
                if (newV.HasValue)
                    mc.VisibleMonth = newV.Value; 
                mc.InvalidateVisibleDays();
                mc.OnSelectedDateChanged(new RoutedPropertyChangedEventArgs<DateTime?>(oldV, newV, SelectedDateChangedEvent));
            }
        }


        /// <summary>
        /// Validate input value in MonthCalendar
        /// </summary>
        /// <returns>Returns False if value isn't null and is outside CalendarDataGenerator.MinDate~MaxDate range.  Otherwise, returns True.</returns>
        private static bool IsValidNullableDate(object value)
        {
            DateTime? date = (DateTime?)value;

            return !date.HasValue || MonthCalendarHelper.IsWithinRange(date.Value, CalendarDataGenerator.MinDate, CalendarDataGenerator.MaxDate);
        }

        #endregion

        #region FirstDayOfWeek

        /// <summary>
        /// The first day of the week as displayed in the month calendar
        /// </summary>
        public DayOfWeek FirstDayOfWeek
        {
            get { return (DayOfWeek)GetValue(FirstDayOfWeekProperty); }
            set { SetValue(FirstDayOfWeekProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for FirstDayOfWeek property
        /// </summary>
        public static readonly DependencyProperty FirstDayOfWeekProperty =
                DependencyProperty.Register(
                        "FirstDayOfWeek",
                        typeof(DayOfWeek),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek /* default value */,
                                new PropertyChangedCallback(OnFirstDayOfWeekChanged)),
                        new ValidateValueCallback(IsValidFirstDayOfWeek));

        private static void OnFirstDayOfWeekChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MonthCalendar)d).InvalidateVisibleDays();
        }

        private static bool IsValidFirstDayOfWeek(object value)
        {
            DayOfWeek day = (DayOfWeek)value;

            return day == DayOfWeek.Sunday
                || day == DayOfWeek.Monday
                || day == DayOfWeek.Tuesday
                || day == DayOfWeek.Wednesday
                || day == DayOfWeek.Thursday
                || day == DayOfWeek.Friday
                || day == DayOfWeek.Saturday;
        }

        #endregion FirstDayOfWeek

        #region Max/MinDate

        /// <summary>
        /// The Property for the MinDate property.
        /// </summary>
        public static readonly DependencyProperty MinDateProperty =
                DependencyProperty.Register(
                        "MinDate",
                        typeof(DateTime),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                CalendarDataGenerator.MinDate, /* The default value */
                                new PropertyChangedCallback(OnMinDateChanged)),
                        new ValidateValueCallback(IsValidDate));

        /// <summary>
        /// The min date of MonthCalendar
        /// </summary>
        public DateTime MinDate
        {
            get { return (DateTime)GetValue(MinDateProperty); }
            set { SetValue(MinDateProperty, value); }
        }

        private static void OnMinDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthCalendar mcc = (MonthCalendar)d;

            DateTime oldMaxDate = mcc.MaxDate;
            DateTime oldVisibleMonth = mcc.VisibleMonth;
            mcc.CoerceValue(MaxDateProperty);
            mcc.CoerceValue(VisibleMonthProperty);

            //If MaxDate, VisibleMonth hasn't been changed by CoerceValue, then 
            //we should update the IsSelectable and SelectedDates in this method
            if (MonthCalendarHelper.CompareYearMonthDay(oldMaxDate, mcc.MaxDate) == 0
                && MonthCalendarHelper.CompareYearMonth(oldVisibleMonth, mcc.VisibleMonth) == 0)
            {
                mcc.OnMaxMinDateChanged((DateTime)e.NewValue, mcc.MaxDate);
            }
        }

        /// <summary>
        /// Validate input value in MonthCalendar (MinDate, MaxDate, VisibleMonth)
        /// </summary>
        /// <returns>Returns False if value is outside CalendarDataGenerator.MinDate~MaxDate range.  Otherwise, returns True.</returns>
        private static bool IsValidDate(object value)
        {
            DateTime date = (DateTime)value;

            return (date >= CalendarDataGenerator.MinDate) &&
                    (date <= CalendarDataGenerator.MaxDate);
        }


        /// <summary>
        /// The Property for the MaxDate property.
        /// </summary>
        public static readonly DependencyProperty MaxDateProperty =
                DependencyProperty.Register(
                        "MaxDate",
                        typeof(DateTime),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                CalendarDataGenerator.MaxDate, /* The default value */
                                new PropertyChangedCallback(OnMaxDateChanged),
                                new CoerceValueCallback(CoerceMaxDate)),
                        new ValidateValueCallback(IsValidDate));

        /// <summary>
        /// The max date of MonthCalendar
        /// </summary>
        public DateTime MaxDate
        {
            get { return (DateTime)GetValue(MaxDateProperty); }
            set { SetValue(MaxDateProperty, value); }
        }

        private static object CoerceMaxDate(DependencyObject d, object value)
        {
            MonthCalendar mcc = (MonthCalendar)d;
            DateTime newValue = (DateTime)value;

            DateTime min = mcc.MinDate;
            if (newValue < min)
            {
                return min;
            }

            return value;
        }

        private static void OnMaxDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthCalendar mcc = (MonthCalendar)d;

            DateTime oldVisibleMonth = mcc.VisibleMonth;
            mcc.CoerceValue(VisibleMonthProperty);

            //If VisibleMonth hasn't been changed by CoerceValue, 
            //we should update the IsSelectable and SelectedDates in this method
            if (MonthCalendarHelper.CompareYearMonth(oldVisibleMonth, mcc.VisibleMonth) == 0)
            {
                mcc.OnMaxMinDateChanged(mcc.MinDate, (DateTime)e.NewValue);
            }
        }

        /// <summary>
        /// Update the IsSelectable property of visible days and selected dates when max/min date has been changed
        /// </summary>
        /// <param name="minDate">new MinDate</param>
        /// <param name="maxDate">new MaxDate</param>
        private void OnMaxMinDateChanged(DateTime minDate, DateTime maxDate)
        {
            int count = VisibleDays.Count;
            for (int i = 0; i < count; ++i)
            {
                VisibleDays[i].IsSelectable =
                    MonthCalendarHelper.IsWithinRange(VisibleDays[i].Date, minDate, maxDate); 
            }

            if (SelectedDate.HasValue && !MonthCalendarHelper.IsWithinRange(SelectedDate.Value, minDate, maxDate))
            {
                SelectedDate = null;
            }
        }

        #endregion

        #region VisibleMonth

        /// <summary>
        /// The DependencyProperty for VisibleMonth property
        /// </summary>
        public static readonly DependencyProperty VisibleMonthProperty =
                DependencyProperty.Register(
                        "VisibleMonth",
                        typeof(DateTime),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                DateTime.Today /* default value */,
                                new PropertyChangedCallback(OnVisibleMonthChanged),
                                new CoerceValueCallback(CoerceVisibleMonth)),
                        //Note: Though only year/month work on VisibleMonth, we'll still compare its value with CalendarDataGenerator.Max/MinDate
                        new ValidateValueCallback(IsValidDate)); 

        /// <summary>
        /// The first visible month
        /// </summary>
        /// <remarks>
        /// Only the Year and Month field is used, not guarantee the day is 1!
        /// </remarks>
        public DateTime VisibleMonth
        {
            get { return (DateTime)GetValue(VisibleMonthProperty); }
            set { SetValue(VisibleMonthProperty, value); }
        }


        private static object CoerceVisibleMonth(DependencyObject d, object value)
        {
            MonthCalendar mcc = (MonthCalendar)d;
            DateTime newValue = (DateTime)value;

            DateTime min = mcc.MinDate;
            if (newValue < min)
            {
                return min;
            }

            DateTime max = mcc.MaxDate;
            if (newValue > max)
            {
                return max;
            }

            return newValue;
        }

        private static void OnVisibleMonthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthCalendar mcc = (MonthCalendar)d;
            DateTime oldDate = (DateTime)e.OldValue;
            DateTime newDate = (DateTime)e.NewValue;

            //oldDate != newDate in Year/Month field
            if (MonthCalendarHelper.CompareYearMonth(oldDate, newDate) != 0)
            {
                mcc.InvalidateVisibleDays();
                mcc.OnVisibleMonthChanged(new RoutedPropertyChangedEventArgs<DateTime>(oldDate, newDate, VisibleMonthChangedEvent));
            }
        }

        #endregion VisibleMonth

        #region ShowsTitle/WeekNumbers/DayHeader Properties

        /// <summary>
        /// The DependencyProperty for ShowsTitle property
        /// </summary>
        public static readonly DependencyProperty ShowsTitleProperty =
                DependencyProperty.Register(
                        "ShowsTitle",
                        typeof(bool),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Indicating whether the control displays the title or not
        /// </summary>
        public bool ShowsTitle
        {
            get { return (bool)GetValue(ShowsTitleProperty); }
            set { SetValue(ShowsTitleProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// The DependencyProperty for ShowsWeekNumbers property
        /// </summary>
        public static readonly DependencyProperty ShowsWeekNumbersProperty =
                DependencyProperty.Register(
                        "ShowsWeekNumbers",
                        typeof(bool),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Indicating whether the control displays the week numbers or not
        /// </summary>
        public bool ShowsWeekNumbers
        {
            get { return (bool)GetValue(ShowsWeekNumbersProperty); }
            set { SetValue(ShowsWeekNumbersProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// The DependencyProperty for ShowsDayHeaders property
        /// </summary>
        public static readonly DependencyProperty ShowsDayHeadersProperty =
                DependencyProperty.Register(
                        "ShowsDayHeaders",
                        typeof(bool),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Indicating whether the control displays the day header or not
        /// </summary>
        public bool ShowsDayHeaders
        {
            get { return (bool)GetValue(ShowsDayHeadersProperty); }
            set { SetValue(ShowsDayHeadersProperty, BooleanBoxes.Box(value)); }
        }

        #endregion

        #region DayTemplate/DayTemplateSelector/DayContainerStyle/DayContainerStyleSelector

        /// <summary>
        /// The DependencyProperty for the DayTemplate property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty DayTemplateProperty =
                DependencyProperty.Register(
                        "DayTemplate",
                        typeof(DataTemplate),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (DataTemplate)null,
                                new PropertyChangedCallback(OnDayTemplateChanged)));

        /// <summary>
        /// DayTemplate is the template that describes how to convert Items into UI elements.
        /// </summary>
        /// <remarks>
        /// DayTemplate must bind with Date, so it can be updated when scroll month
        /// here is a sample:
        /// &lt;DataTemplate x:Key="dayTemplate"&gt;
        ///     &lt;TextBlock Text="{Binding Date}"/&gt;
        /// &lt;/DataTemplate&gt;
        /// </remarks>
        public DataTemplate DayTemplate
        {
            get { return (DataTemplate)GetValue(DayTemplateProperty); }
            set { SetValue(DayTemplateProperty, value); }
        }

        /// <summary>
        /// Called when DayTemplateProperty is invalidated on "d."
        /// </summary>
        private static void OnDayTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthCalendar mcc = (MonthCalendar)d;
            mcc.RefreshDayTemplate();
        }

        /// <summary>
        /// The DependencyProperty for the DayTemplateSelector property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty DayTemplateSelectorProperty =
                DependencyProperty.Register(
                        "DayTemplateSelector",
                        typeof(DataTemplateSelector),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (DataTemplateSelector)null,
                                new PropertyChangedCallback(OnDayTemplateSelectorChanged)));

        /// <summary>
        /// DayTemplateSelector allows the app writer to provide custom template selection logic
        /// for a template to apply to each item.
        /// </summary>
        public DataTemplateSelector DayTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(DayTemplateSelectorProperty); }
            set { SetValue(DayTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Called when DayTemplateSelectorProperty is invalidated on "d."
        /// </summary>
        private static void OnDayTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthCalendar mcc = (MonthCalendar)d;
            mcc.RefreshDayTemplate();
        }

        /// <summary>
        /// The DependencyProperty for the DayContainerStyle property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty DayContainerStyleProperty =
                DependencyProperty.Register(
                        "DayContainerStyle",
                        typeof(Style),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (Style)null,
                                new PropertyChangedCallback(OnDayContainerStyleChanged)));

        /// <summary>
        /// 
        /// </summary>
        public Style DayContainerStyle
        {
            get { return (Style)GetValue(DayContainerStyleProperty); }
            set { SetValue(DayContainerStyleProperty, value); }
        }

        /// <summary>
        /// Called when DayContainerStyleProperty is invalidated on "d."
        /// </summary>
        private static void OnDayContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MonthCalendar)d).RefreshDayTemplate();
        }

        /// <summary>
        ///     The DependencyProperty for the DayContainerStyleSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty DayContainerStyleSelectorProperty =
                DependencyProperty.Register(
                        "DayContainerStyleSelector",
                        typeof(StyleSelector),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (StyleSelector)null,
                                new PropertyChangedCallback(OnDayContainerStyleChanged)));

        /// <summary>
        ///     DayContainerStyleSelector allows the application writer to provide custom logic
        ///     to choose the style to apply to each generated day element.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="DayContainerStyle"/> is set.
        /// </remarks>
        public StyleSelector DayContainerStyleSelector
        {
            get { return (StyleSelector)GetValue(DayContainerStyleSelectorProperty); }
            set { SetValue(DayContainerStyleSelectorProperty, value); }
        }

        #endregion

        #region TitleStyle/DayHeaderStyle/WeekNumberStyle

        /// <summary>
        /// The DependencyProperty for the TitleStyle property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty TitleStyleProperty =
                DependencyProperty.Register(
                        "TitleStyle",
                        typeof(Style),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (Style)null));

        /// <summary>
        /// TitleStyle property
        /// </summary>
        public Style TitleStyle
        {
            get { return (Style)GetValue(TitleStyleProperty); }
            set { SetValue(TitleStyleProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for the DayHeaderStyle property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty DayHeaderStyleProperty =
                DependencyProperty.Register(
                        "DayHeaderStyle",
                        typeof(Style),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (Style)null));

        /// <summary>
        /// DayHeaderStyle property
        /// </summary>
        public Style DayHeaderStyle
        {
            get { return (Style)GetValue(DayHeaderStyleProperty); }
            set { SetValue(DayHeaderStyleProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for the WeekNumberStyle property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty WeekNumberStyleProperty =
                DependencyProperty.Register(
                        "WeekNumberStyle",
                        typeof(Style),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (Style)null));

        /// <summary>
        /// WeekNumberStyle property
        /// </summary>
        public Style WeekNumberStyle
        {
            get { return (Style)GetValue(WeekNumberStyleProperty); }
            set { SetValue(WeekNumberStyleProperty, value); }
        }

        #endregion

        #region Previous/NextButtonStyle

        /// <summary>
        /// The DependencyProperty for the PreviousButtonStyle property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty PreviousButtonStyleProperty =
                DependencyProperty.Register(
                        "PreviousButtonStyle",
                        typeof(Style),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (Style)null, new PropertyChangedCallback(OnPreviousButtonStyleChanged)));

        /// <summary>
        /// PreviousButtonStyle property
        /// </summary>
        public Style PreviousButtonStyle
        {
            get { return (Style)GetValue(PreviousButtonStyleProperty); }
            set { SetValue(PreviousButtonStyleProperty, value); }
        }

        private static void OnPreviousButtonStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MonthCalendar)d).RefreshPreviousButtonStyle();
        }

        /// <summary>
        /// The DependencyProperty for the NextButtonStyle property.
        /// Flags:              none
        /// Default Value:      null
        /// </summary>
        public static readonly DependencyProperty NextButtonStyleProperty =
                DependencyProperty.Register(
                        "NextButtonStyle",
                        typeof(Style),
                        typeof(MonthCalendar),
                        new FrameworkPropertyMetadata(
                                (Style)null, new PropertyChangedCallback(OnNextButtonStyleChanged)));

        /// <summary>
        /// NextButtonStyle property
        /// </summary>
        public Style NextButtonStyle
        {
            get { return (Style)GetValue(NextButtonStyleProperty); }
            set { SetValue(NextButtonStyleProperty, value); }
        }

        private static void OnNextButtonStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MonthCalendar)d).RefreshNextButtonStyle();
        }

        #endregion


        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Raise VisibleMonthChanged event.
        /// </summary>
        /// <param name="e">RoutedPropertyChangedEventArgs contains the old and new value.</param>
        protected virtual void OnVisibleMonthChanged(RoutedPropertyChangedEventArgs<DateTime> e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Raise SelecteDateChanged event.
        /// </summary>
        protected virtual void OnSelectedDateChanged(RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// This is the method that responds to the PreviewKeyDown event.
        /// </summary>
        /// <param name="e">Event Arguments</param>
        /// <remarks>
        /// Override OnPreviewKeyDown isn't recommended for Control Author, it's reserved for customer
        /// Because MonthCalenarContainer already handles the PageUp/PageDown/Home/End, we have to use Preview here.
        /// </remarks>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true; 
            switch (e.Key)
            {
                case Key.PageUp:
                    PreviousCommand.Execute(null, this);
                    break;
                case Key.PageDown:
                    NextCommand.Execute(null, this);
                    break;
            }
            if (SelectedDate != null)
            {
                DateTime date = SelectedDate.Value;
                switch (e.Key)
                {
                    case Key.Up:
                        SelectedDate = date.AddDays(-7);
                        break;
                    case Key.Down:
                        SelectedDate = date.AddDays(7);
                        break;
                    case Key.Left:
                        SelectedDate = date.AddDays(-1);
                        break;
                    case Key.Right:
                        SelectedDate = date.AddDays(1);
                        break;
                }

            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Generate the visible days collection based on the input firstdate, lastdate and firstdayofweek
        /// </summary>
        private ObservableCollection<CalendarDate> CreateVisibleDaysCollection(DateTime firstDate, DateTime lastDate, DayOfWeek firstDayOfWeek)
        {
            DateTime leadingDate = CalendarDataGenerator.CalculateLeadingDate(firstDate, firstDayOfWeek);
            DateTime trailingDate = CalendarDataGenerator.CalculateTrailingDate(firstDate, lastDate, firstDayOfWeek);
            int totalDay = trailingDate.Subtract(leadingDate).Days + 1;

            ObservableCollection<CalendarDate> collection = new ObservableCollection<CalendarDate>();
            for (int i = 0; i < totalDay; ++i)
            {
                CalendarDate cdate = new CalendarDate(leadingDate.AddDays(i));
                cdate.IsOtherMonth = cdate.Date < FirstDate || cdate.Date > LastDate;
                cdate.IsSelectable = MonthCalendarHelper.IsWithinRange(cdate.Date, MinDate, MaxDate);

                collection.Add(cdate);
            }

            return collection;
        }

        /// <summary>
        /// Invalidate the visible days when switch month
        /// </summary>
        /// <param name="scrollChange"></param>
        private void InvalidateVisibleDays()
        {
            if (_mccContainer != null)
            {
                ObservableCollection<CalendarDate> newVisibleDays = CreateVisibleDaysCollection(FirstDate, LastDate, FirstDayOfWeek);
                Debug.Assert(newVisibleDays.Count == 42);
                VisibleDays = newVisibleDays;
                _mccContainer.ItemsSource = newVisibleDays;
                _mccContainer.SelectedItem = SelectedDate.TrySC(sd=>newVisibleDays.SingleOrDefault(c => c.Date == sd));
            }
        }


        /// <summary>
        /// Scroll the current visible month of MothCalendar based on delta and direction
        /// </summary>
        /// <param name="direction">1:increase; -1: decrease</param>
        /// <param name="delta">how many months should be scrolled, 0 means use the default value</param>
        private void ScrollVisibleMonth(int direction, int delta)
        {
            //NOTE:
            // To read the graph below, please use fixed width font.
            //
            // Date range:                              Min                               Max
            //                                           |--------------------------------|
            //                                           .                                .
            // Valid cases:                              .                                .
            //                                           .                                .
            // Case 1:                                   .     [..................]       .
            //                                           .  FstVsM                        .
            //                                           .                                .
            // Case 2:                                   [..................]             .
            //                                        FstVsM                              .
            //                                           .                                .
            // Case 3:                                   .             [..................]
            //                                           .          FstVsM                .
            //                                           .                                .
            // Case 4:                                   [.........................................]
            //                                        FstVsM                              .
            //                                           .                                .
            //                                           .                                .
            // Case 5:                                   .             [.......................]
            //                                           .           FstVsM               .
            //                                           .                                .
            // Valid initial but no returnable cases:    .                                .
            //                                           .                                .
            // Case 6:                      [..................]                          .
            //                           FstVsM                                           .
            //                                           .                                .
            // Case 7:                                   .                                .       [..................]
            //                                           .                                .    FstVsM
            //
            // Invalid case(s)
            //                                           .                                .
            // Case 8:                          [.........................................]
            //                               FstVsM

            int monthCount = 1;

            if (delta == 0)
            {
                delta = monthCount;
            }

            DateTime firstMonth = VisibleMonth;
            bool checkFirstMonth = false;

            // Scroll to the previous page
            if (direction == -1)
            {
                if (MonthCalendarHelper.CompareYearMonth(VisibleMonth, MinDate) > 0) // case 1/3/5
                {
                    checkFirstMonth = true;
                    try
                    {
                        firstMonth = VisibleMonth.AddMonths(delta * direction);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        firstMonth = MinDate; // case 2/4
                    }
                }
            }
            // Scroll to the next page
            else
            {
                try
                {
                    if (MonthCalendarHelper.CompareYearMonth(LastDate, MaxDate) < 0) // case 1-4
                    {
                        firstMonth = VisibleMonth.AddMonths(delta * direction);
                        DateTime lastMonth = firstMonth.AddMonths(monthCount - 1);

                        // if lastMonth is greater than MaxDate, scroll back to the appropriate position
                        if (MonthCalendarHelper.CompareYearMonth(lastMonth, MaxDate) > 0) // case 5
                        {
                            checkFirstMonth = true;
                            firstMonth = MaxDate.AddMonths(-(monthCount - 1)); // case 3
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    checkFirstMonth = true;
                    firstMonth = MaxDate.AddMonths(-(monthCount - 1)); // case 3
                }
            }

            // check in case the firstMonth is less than MinDate
            if (checkFirstMonth && MonthCalendarHelper.CompareYearMonth(firstMonth, MinDate) < 0) // case 8
            {
                firstMonth = MinDate; // change to case 4
            }

            VisibleMonth = firstMonth;
        }

  
      

        /// <summary>
        /// Refresh the ItemTemplate/ItemTemplateSelector/ItemContainerStyle if DayTemplate/DayTemplateSelecotr/DayContainerStyle is set
        /// </summary>
        private void RefreshDayTemplate()
        {
            if (_mccContainer != null)
            {
                //if both DayTemplate/Selector is null, use the default DayTemplate
                if (DayTemplate == null && DayTemplateSelector == null)
                {
                    if (s_MonthCalendarDayTemplate == null)
                    {
                        s_MonthCalendarDayTemplate = new DataTemplate();
                        FrameworkElementFactory txt = new FrameworkElementFactory(typeof(TextBlock));
                        txt.SetBinding(TextBlock.TextProperty, new Binding("Date.Day"));
                        s_MonthCalendarDayTemplate.VisualTree = txt;
                    }

                    _mccContainer.ItemTemplateSelector = null;
                    _mccContainer.ItemTemplate = s_MonthCalendarDayTemplate;
                }
                else
                {
                    _mccContainer.ItemTemplate = DayTemplate;
                    _mccContainer.ItemTemplateSelector = DayTemplateSelector;
                }

                //SetFlag(Flags.IsVisibleDaysUpdated, true);
            }
        }

        private void RefreshPreviousButtonStyle()
        {
            ButtonBase previousButton = GetTemplateChild(c_PreviousButtonName) as ButtonBase;
            if (previousButton != null)
            {
                if (PreviousButtonStyle == null)
                {
                    if (_defaultPreviousButtonStyle == null)
                    {
                        _defaultPreviousButtonStyle = FindResource(new ComponentResourceKey(typeof(MonthCalendar), "PreviousButtonStyleKey")) as Style;
                    }
                    previousButton.Style = _defaultPreviousButtonStyle;
                }
                else
                {
                    previousButton.Style = PreviousButtonStyle;
                }
            }
        }

        private void RefreshNextButtonStyle()
        {
            ButtonBase nextButton = GetTemplateChild(c_NextButtonName) as ButtonBase;
            if (nextButton != null)
            {
                if (NextButtonStyle == null)
                {
                    if (_defaultNextButtonStyle == null)
                    {
                        _defaultNextButtonStyle = FindResource(new ComponentResourceKey(typeof(MonthCalendar), "NextButtonStyleKey")) as Style;
                    }
                    nextButton.Style = _defaultNextButtonStyle;
                }
                else
                {
                    nextButton.Style = NextButtonStyle;
                }
            }
        }
     
        #region Selection

        /// <summary>
        /// Get the CalendarDate by date
        /// </summary>
        /// <returns>null if the date exceeds First/LastVisibleDate</returns>
        private CalendarDate GetCalendarDateByDate(DateTime date)
        {
            TimeSpan ts = date - FirstVisibleDate;
            return VisibleDays.Count > ts.Days && ts.Days >= 0 ? VisibleDays[ts.Days] : null;
        }

   
        /// <summary>
        /// Update the selected dates status when user changes it by UI
        /// </summary>
        private void OnContainerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mccContainer.TemplatedParent == null || e.OriginalSource != _mccContainer)
                return;

            if (_mccContainer.SelectedItem != null)
                SelectedDate = ((CalendarDate)_mccContainer.SelectedItem).Date;
        }


        #endregion

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private MonthCalendarContainer _mccContainer;

        private DateTime FirstVisibleDate { get { return VisibleDays[0].Date; } }

        private DateTime LastVisibleDate { get { return VisibleDays[VisibleDays.Count - 1].Date; } }

        /// <summary>
        /// first day of the first currently visible month
        /// </summary>
        private DateTime FirstDate
        {
            get { return new DateTime(VisibleMonth.Year, VisibleMonth.Month, 1); }
        }

        /// <summary>
        /// last day of the last currently visible month
        /// </summary>
        private DateTime LastDate
        {
            get { return FirstDate.AddMonths(1).AddDays(-1); }
        }

        private ObservableCollection<CalendarDate> VisibleDays
        {
            get 
            {
                if (_visibleDays == null)
                {
                    _visibleDays = CreateVisibleDaysCollection(FirstDate, LastDate, FirstDayOfWeek);
                }
                return _visibleDays;
            }
            set 
            {
                _visibleDays = value;
            }
        }

        private ObservableCollection<CalendarDate> _visibleDays;


        private Style _defaultPreviousButtonStyle, _defaultNextButtonStyle;
        private static DataTemplate s_MonthCalendarDayTemplate;

        // Part name used in the style. The class TemplatePartAttribute should use the same name
        private const string c_VisibleDaysHostTemplateName = "PART_VisibleDaysHost";
        private const string c_PreviousButtonName = "PART_PreviousButton";
        private const string c_NextButtonName = "PART_NextButton";

        private const string c_exRangeActionsNotSupported = "Range actions are not supported.";

        #endregion

    }
}