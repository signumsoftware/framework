//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.ComponentModel;


namespace Signum.Windows.DateUtils
{
    /// <summary>
    /// CalendarDate is a wrapper over DateTime
    /// </summary>
    /// <remarks>
    /// The main purpose of CalendarDate is to increase the MonthCalendar's scroll perf.
    /// It is used to only update Date object and fire a PropertyChanged event, 
    /// so when user scroll month, the selectedDates UI items won't be regenerated
    /// </remarks>
    public sealed class CalendarDate : INotifyPropertyChanged
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public CalendarDate(DateTime date)
        {
            _date = date;
        }

        #endregion

        #region Public Properties/Events/Methods

        /// <summary>
        /// This event is raised when CalendarDate.Date has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
            internal set
            {
                if (_date != value)
                {
                    DateTime oldDate = _date;
                    _date = value;
                    OnPropertyChanged("Date");
                    if (IsDateToday(oldDate) != IsDateToday(_date))
                    {
                        OnPropertyChanged("IsToday");
                    }

                    if (IsDateWeekend(oldDate) != IsDateWeekend(_date))
                    {
                        OnPropertyChanged("IsWeekend");
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether this selectedDate is a month other than the month displayed in the MonthCalendar control
        /// </summary>
        public bool IsOtherMonth
        {
            get { return _isOtherMonth; }
            internal set
            {
                if (_isOtherMonth != value)
                {
                    _isOtherMonth = value;
                    OnPropertyChanged("IsOtherMonth");
                }
            }
        }

        /// <summary>
        /// Indicates whether this selectedDate should be enabled or not. (This value is used to disable the selectedDate out of Max/MinDate)
        /// </summary>
        public bool IsSelectable
        {
            get { return _isSelectable; }
            internal set
            {
                if (_isSelectable != value)
                {
                    _isSelectable = value;
                    OnPropertyChanged("IsSelectable");
                }
            }
        }

        /// <summary>
        /// Indicates wheter the selectedDate is the same selectedDate specified by the 
        /// </summary>
        public bool IsToday
        {
            get { return IsDateToday(_date); }
        }

        /// <summary>
        /// Indicates whether the selectedDate is a either Saturday or Sunday
        /// </summary>
        public bool IsWeekend
        {
            get { return IsDateWeekend(_date); }
        }      

        /// <summary>
        /// Return string representation of this selectedDate
        /// </summary>
        public override string ToString()
        {
            return _date.ToShortDateString();
        }

        #endregion

        #region Internal Properties/Methods

        /// <summary>
        /// Update Date and IsValid property
        /// </summary>
        internal void InternalUpdate(CalendarDate cdate)
        {
            Date = cdate.Date;
            IsOtherMonth = cdate.IsOtherMonth;
            IsSelectable = cdate.IsSelectable;
        }

        #endregion

        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        private bool IsDateWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        private bool IsDateToday(DateTime date)
        {
            return MonthCalendarHelper.CompareYearMonthDay(date, DateTime.Now) == 0;
        }

        private DateTime _date;
        private bool _isOtherMonth;
        private bool _isSelectable = true;
    }
}