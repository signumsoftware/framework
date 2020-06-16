using System;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class DateSpanEmbedded : EmbeddedEntity
    {
        public int Years { get; set; }

        public int Months { get; set; }

        public int Days { get; set; }

        public bool IsZero()
        {
            return Years == 0 && Months == 0 && Days == 0;
        }

        [AutoExpressionField]
        public DateTime Add(DateTime date) => As.Expression(() => date.AddYears(Years).AddMonths(Months).AddDays(Days));

        [AutoExpressionField]
        public DateTime Subtract(DateTime date) => As.Expression(() => date.AddYears(-Years).AddMonths(-Months).AddDays(-Days));

        public DateSpan ToDateSpan()
        {
            return new DateSpan(Years, Months, Days);
        }

        public override string ToString()
        {
            return ToDateSpan().ToString();
        }

        public DateSpanEmbedded Clone()
        {

            DateSpanEmbedded ds = new DateSpanEmbedded
            {
                Days = this.Days,
                Months = this.Months,
                Years = this.Years,
            };

            return ds;
        }
    }

    [Serializable]
    public class TimeSpanEmbedded : EmbeddedEntity
    {
        public int Days { get; set; }

        public int Hours { get; set; }

        public int Minutes { get; set; }

        public int Seconds { get; set; }

        public bool IsZero()
        {
            return Days == 0 && Hours == 0 && Minutes == 0  && Seconds == 0;
        }

        [AutoExpressionField]
        public DateTime Add(DateTime date) => As.Expression(() => date.AddDays(Days).AddHours(Hours).AddMinutes(Minutes).AddSeconds(Seconds));

        [AutoExpressionField]
        public DateTime Subtract(DateTime date) => As.Expression(() => date.AddDays(-Days).AddHours(-Hours).AddMinutes(-Minutes).AddMinutes(-Seconds));

        public TimeSpan ToTimeSpan()
        {
            return new TimeSpan(Days, Hours, Minutes, Seconds);
        }

        public override string ToString()
        {
            return ToTimeSpan().ToString();
        }

        public TimeSpanEmbedded Clone()
        {
            TimeSpanEmbedded ds = new TimeSpanEmbedded
            {
                Days = this.Days,
                Hours = this.Hours,
                Minutes = this.Minutes,
                Seconds = this.Seconds,
            };

            return ds;
        }
    }
}
