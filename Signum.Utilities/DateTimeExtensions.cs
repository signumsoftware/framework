using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Properties;
using System.Globalization;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Utilities
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander1))]
        public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime maxDate)
        {
            return minDate <= date && date < maxDate;
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander2))]
        public static bool IsInInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
        {
            return minDate <= date && (maxDate == null || date < maxDate);
        }

        /// <summary>
        /// Checks if the date is inside a C interval defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander3))]
        public static bool IsInInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
        {
            return (minDate == null || minDate <= date) && 
                   (maxDate == null || date < maxDate); 
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
        [MethodExpander(typeof(IsInIntervalExpander1))]
        public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime maxDate)
        {
            AssertDateOnly(date, minDate, maxDate);
            return minDate <= date && date <= maxDate;
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander2))]
        public static bool IsInDateInterval(this DateTime date, DateTime minDate, DateTime? maxDate)
        {
            AssertDateOnly(date, minDate, maxDate);
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate); 
        }

        /// <summary>
        /// Checks if the date is inside a date-only interval (compared by entires days) defined by the two given dates
        /// </summary>
        [MethodExpander(typeof(IsInIntervalExpander3))]
        public static bool IsInDateInterval(this DateTime date, DateTime? minDate, DateTime? maxDate)
        {
            AssertDateOnly(date, minDate, maxDate);
            return (minDate == null || minDate <= date) &&
                   (maxDate == null || date < maxDate); 
        }

        class IsInIntervalExpander1 : IMethodExpander
        {
            static readonly Expression<Func<DateTime, DateTime, DateTime, bool>> func = (date, minDate, maxDate) => minDate <= date && date < maxDate;

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        class IsInIntervalExpander2 : IMethodExpander
        {
            Expression<Func<DateTime, DateTime, DateTime?, bool>> func = (date, minDate, maxDate) => minDate <= date && (maxDate == null || date < maxDate);

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
        }

        class IsInIntervalExpander3 : IMethodExpander
        {
            Expression<Func<DateTime, DateTime?, DateTime?, bool>> func = (date, minDate, maxDate) =>
                (minDate == null || minDate <= date) &&
                (maxDate == null || date < maxDate);

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return Expression.Invoke(func, arguments[0], arguments[1], arguments[2]);
            }
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
    }

    public struct DateSpan
    {
        public static readonly DateSpan Zero = new DateSpan(0, 0, 0);

        public readonly int Years;
        public readonly int Months;
        public readonly int Days;

        public DateSpan(int years, int months, int days)
        {
            this.Years = years;
            this.Months = months;
            this.Days = days;
        }

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
            return ", ".Combine(
                         Years == 0 ? null :
                         Years == 1 ? Resources._0Year.Formato(Years) :
                                     Resources._0Years.Formato(Years),
                         Months == 0 ? null :
                         Months == 1 ? Resources._0Month.Formato(Years) :
                                      Resources._0Months.Formato(Years),
                         Days == 0 ? null :
                         Days == 1 ? Resources._0Day.Formato(Years) :
                                    Resources._0Days.Formato(Years));
        }
    }
}
