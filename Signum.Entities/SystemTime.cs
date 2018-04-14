using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities
{
    public abstract class SystemTime
    {
        static Variable<SystemTime> currentVariable = Statics.ThreadVariable<SystemTime>("systemTime");

        public static SystemTime Current => currentVariable.Value;


        public static IDisposable Override(DateTime asOf) => Override(new SystemTime.AsOf(asOf));
        public static IDisposable Override(SystemTime systemTime)
        {
            var old = currentVariable.Value;
            currentVariable.Value = systemTime;
            return new Disposable(() => currentVariable.Value = old);
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
                this.DateTime = dateTime;
            }

            public override string ToString() => $"AS OF {DateTime:u}";
        }

        public abstract class Interval : SystemTime
        {

        }

        public class FromTo : Interval
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            public FromTo(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
            }

            public override string ToString() => $"FROM {StartDateTime:u} TO {EndtDateTime:u}";
        }

        public class Between : Interval
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            public Between(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
            }

            public override string ToString() => $"BETWEEN {StartDateTime:u} AND {EndtDateTime:u}";
        }

        public class ContainedIn : Interval
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            public ContainedIn(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
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


        static MethodInfo miOverlaps = ReflectionTools.GetMethodInfo((Interval<DateTime> pair) => pair.Overlaps(new Interval<DateTime>()));
        internal static Expression Overlaps(this NewExpression interval1, NewExpression interval2)
        {
            if (interval1 == null)
                return null;

            if (interval2 == null)
                return null;

            var min1 = interval1.Arguments[0];
            var max1 = interval1.Arguments[1];
            var min2 = interval2.Arguments[0];
            var max2 = interval2.Arguments[1];

            return Expression.And(
                 Expression.GreaterThan(max1, min2),
                 Expression.GreaterThan(max2, min1)
                 );
        }



        static ConstructorInfo ciInterval = ReflectionTools.GetConstuctorInfo(() => new Interval<DateTime>(new DateTime(), new DateTime()));
        internal static Expression Intesection(this NewExpression interval1, NewExpression interval2)
        {
            if (interval1 == null)
                return interval2;

            if (interval2 == null)
                return interval1;

            var min1 = interval1.Arguments[0];
            var max1 = interval1.Arguments[1];
            var min2 = interval2.Arguments[0];
            var max2 = interval2.Arguments[1];

            return Expression.New(ciInterval,
                  Expression.Condition(Expression.LessThan(min1, min2), min1, min2),
                  Expression.Condition(Expression.GreaterThan(max1, max2), max1, max2));
        }

        public static Expression And(this Expression expression, Expression other)
        {
            if (other == null)
                return expression;

            return Expression.And(expression, other);
        }
    }
}
