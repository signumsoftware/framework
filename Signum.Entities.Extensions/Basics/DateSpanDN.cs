using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Extensions.Basics
{
    [Serializable]
    public class DateSpanDN : EmbeddedEntity
    {
        int years;
        public int Years
        {
            get { return years; }
            set { Set(ref years, value, "Years"); }
        }

        int months;
        public int Months
        {
            get { return months; }
            set { Set(ref months, value, "Months"); }
        }

        int days;
        public int Days
        {
            get { return days; }
            set { Set(ref days, value, "Days"); }
        }

        public DateTime Add(DateTime date)
        {
            return date.AddYears(years).AddMonths(months).AddDays(days);
        }

    }
}
