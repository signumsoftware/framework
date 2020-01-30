using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities
{
    public abstract class SystemTime
    {
        static Variable<SystemTime?> currentVariable = Statics.ThreadVariable<SystemTime?>("systemTime");

        public static SystemTime? Current => currentVariable.Value;

        public static IDisposable Override(DateTime asOf) => Override(new SystemTime.AsOf(asOf));
        public static IDisposable Override(SystemTime? systemTime)
        {
            var old = currentVariable.Value;
            currentVariable.Value = systemTime;
            return new Disposable(() => currentVariable.Value = old);
        }

        static DateTime ValidateUTC(DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("Date should be in UTC");

            return dt;
        }

        public class HistoryTable : SystemTime
        {
            public override string ToString() => "HistoryTable";
        }

        public class AsOf : SystemTime
        {
            public DateTime DateTime { get; private set; }

            public AsOf(DateTime dateTime)
            {
                this.DateTime = ValidateUTC(dateTime);
            }

            public override string ToString() => $"AS OF {DateTime:u}";
        }

        public abstract class Interval : SystemTime
        {


        }

        public class Between : Interval
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            public Between(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = ValidateUTC(startDateTime);
                this.EndtDateTime = ValidateUTC(endDateTime);
            }

            public override string ToString() => $"BETWEEN {StartDateTime:u} AND {EndtDateTime:u}";
        }

        public class ContainedIn : Interval
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            public ContainedIn(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = ValidateUTC(startDateTime);
                this.EndtDateTime = ValidateUTC(endDateTime);
            }

            public override string ToString() => $"CONTAINED IN ({StartDateTime:u}, {EndtDateTime:u})";
        }

        public class All : Interval
        {
            public All()
            {
            }

            public override string ToString() => $"ALL";
        }
    }

    public static class SystemTimeExtensions
    {
        public static Interval<DateTime> SystemPeriod(this Entity entity)
        {
            throw new InvalidOperationException("Only for queries");
        }

        public static Interval<DateTime> SystemPeriod<E, T>(this MListElement<E, T> mlistElement)
            where E : Entity
        {
            throw new InvalidOperationException("Only for queries");
        }
    }
}
