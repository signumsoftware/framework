using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Properties;
using System.Globalization;
using System.Linq.Expressions;

namespace Signum.Utilities
{
    public static class DateTimeExtensions
    {
        public static Expression<Func<DateTime, DateTime, DateTime, bool>> IsInIntervalExpression =
            (date, firstDate, lastDate) => firstDate <= date && date < lastDate;

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        public static bool IsInInterval(this DateTime date, DateTime firstDate, DateTime lastDate)
        {
            return firstDate <= date && date < lastDate;
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        public static bool IsInInterval(this DateTime date, DateTime firstDate, DateTime? lastDate)
        {
            return date.IsInInterval(firstDate, lastDate ?? DateTime.MinValue);
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        public static bool IsInInterval(this DateTime date, DateTime? firstDate, DateTime? lastDate)
        {
            return date.IsInInterval(firstDate ?? DateTime.MinValue, lastDate ?? DateTime.MaxValue);
        }

        private static void AssertDateOnly(params DateTime?[] args)
        {
            foreach (DateTime d in args.Where(dt => dt.HasValue))
            {
                if (d.Hour != 0 || d.Minute != 0 || d.Second != 0 || d.Millisecond != 0)
                    throw new ApplicationException(Resources.TheDateHasSomeValueInTheHourMinuteSecondOrMillisecond);
            }
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        public static bool IsInDateInterval(this DateTime date, DateTime firstDate, DateTime lastDate)
        {
            AssertDateOnly(date, firstDate, lastDate);
            if (firstDate <= date && date <= lastDate)
                return true;
            return false;
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        public static bool IsInDateInterval(this DateTime date, DateTime firstDate, DateTime? lastDate)
        {
            return date.IsInDateInterval(firstDate, lastDate ?? DateTime.MinValue.Date);
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        public static bool IsInDateInterval(this DateTime date, DateTime? firstDate, DateTime? lastDate)
        {
            return date.IsInDateInterval(firstDate ?? DateTime.MinValue.Date, lastDate ?? DateTime.MaxValue.Date);
        }

        public static int YearsTo(this DateTime min, DateTime max)
        {
            int result = max.Year - min.Year;
            if (max.Month < min.Month || (max.Month == min.Month & max.Day < min.Day))
                result--;

            return result;
        }

        public static int MonthsTo(this DateTime min, DateTime max)
        {
            int result = max.Month - min.Month + (max.Year - min.Year) * 12;
            if (max.Day < min.Day)
                result--;

            return result;
        }

        public static DateSpan DateSpanTo(this DateTime min, DateTime max)
        {
            return DateSpan.FromToDates(min, max);
        }

        public static DateTime Add(this DateTime date, DateSpan dateSpan)
        {
            return dateSpan.AddTo(date);
        }

        public static DateTime Min(DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        public static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        public static string ShortDateTimePattern(this DateTimeFormatInfo dtfi)
        {
            return dtfi.ShortDatePattern + " " + dtfi.ShortTimePattern;
        }

        public static string ToShortDateTimeString(this DateTime dt)
        {
            return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDateTimePattern());
        }
    }

    public struct DateSpan
    {
        public static readonly DateSpan Zero = new DateSpan(0, 0, 0);

        public readonly int Years;
        public readonly int Months;
        public readonly int Days;
      /*  public readonly int Hours;
        public readonly int Minutes;
        public readonly int Seconds;*/

        public DateSpan(int years, int months, int days)
        {
            this.Years = years;
            this.Months = months;
            this.Days = days;
      /*      this.Hours = 0;
            this.Minutes = 0;
            this.Seconds = 0;*/
        }

     /*   public DateSpan(int years, int months, int days, int hours, int minutes, int seconds)
        {
            this.Years = years;
            this.Months = months;
            this.Days = days;
            this.Hours = hours;
            this.Minutes = minutes;
            this.Seconds = seconds;
        }*/

        public static DateSpan FromToDates(DateTime min, DateTime max)
        {
            if (min > max) return FromToDates(max, min).Invert();

            int years = max.Year - min.Year;
            int months = max.Month - min.Month;


            if (max.Day < min.Day)
                months -= 1;

            if (months < 0)
            {
                years -= 1;
                months += 12;
            }

            int days = max.Subtract(min.AddYears(years).AddMonths(months)).Days;

            /*
            int hours = max.Subtract(min.AddYears(years).AddMonths(months)).Hours;
            int minutes = max.Subtract(min.AddYears(years).AddMonths(months)).Minutes;
            int seconds = max.Subtract(min.AddYears(years).AddMonths(months)).Seconds;

            return new DateSpan(years, months, days, hours, minutes, seconds);
            */
            return new DateSpan(years, months, days);
        }

        public DateTime AddTo(DateTime date)
        {
            return date.AddDays(Days).AddMonths(Months).AddYears(Years);
        }

        public DateSpan Invert()
        {
            return new DateSpan(-Years, -Months, -Days);
        }

        public override string ToString()
        {
            string s = string.Empty;
            string separator = ", ";
            if (Years > 0)
            {
                if (Years == 1) s=s.Add(Properties.Resources._0Year.Formato(Years), separator);
                else s = s.Add(Properties.Resources._0Years.Formato(Years), separator);
            }

            if (Months > 0)
            {
                if (Months == 1) s = s.Add(Properties.Resources._0Month.Formato(Months), separator);
                else s = s.Add(Properties.Resources._0Months.Formato(Months), separator);
            }

            if (Days > 0)
            {
                if (Days == 1) s = s.Add(Properties.Resources._0Day.Formato(Days), separator);
                else s = s.Add(Properties.Resources._0Days.Formato(Days), separator);
            }

            return s;
        }

       /* public string ToShortString()
        {
            string s = string.Empty;
            string separator = ", ";
            if (Years > 0)
            {
                if (Years == 1) s = s.Add(Properties.Resources._0Year.Formato(Years), separator);
                else s = s.Add(Properties.Resources._0Years.Formato(Years), separator);
                return s;
            }

            if (Months > 0)
            {
                if (Months == 1) s = s.Add(Properties.Resources._0Month.Formato(Months), separator);
                else s = s.Add(Properties.Resources._0Months.Formato(Months), separator);
                return s;
            }

            if (Days > 0)
            {
                if (Days == 1) s = s.Add(Properties.Resources._0Day.Formato(Days), separator);
                else s = s.Add(Properties.Resources._0Days.Formato(Days), separator);
                return s;
            }

            if (Hours > 0)
            {
                if (Hours == 1) s = s.Add(Properties.Resources._0Hour.Formato(Hours), separator);
                else s = s.Add(Properties.Resources._0Hours.Formato(Hours), separator);
                return s;
            }

            if (Minutes > 0)
            {
                if (Minutes == 1) s = s.Add(Properties.Resources._0Minute.Formato(Minutes), separator);
                else s = s.Add(Properties.Resources._0Minutes.Formato(Minutes), separator);
                return s;
            }

            if (Seconds > 0)
            {
                if (Seconds == 1) s = s.Add(Properties.Resources._0Second.Formato(Seconds), separator);
                else s = s.Add(Properties.Resources._0Seconds.Formato(Seconds), separator);
                return s;
            }
            return s;
        }*/
    }
}
