using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class DateTimeExtensions
    {
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
    }

    public struct DateSpan
    {
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

            int yeas = max.Year - min.Year;
            int months = max.Month - min.Month;
          
        
            if (max.Day < min.Day)
                months -= 1;

            if (months < 0)
            {
                yeas -= 1;
                months += 12;
            }

            int days = max.Subtract(min.AddYears(yeas).AddMonths(months)).Days;

            return new DateSpan(yeas, months, days);

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
            return Properties.Resources._0Year1Month2Day.Formato(Years, Months, Days);
        }
    }
}
