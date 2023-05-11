using Signum.Utilities.DataStructures;

namespace Signum.Entities;

public abstract class SystemTime
{
    static Variable<SystemTime?> currentVariable = Statics.ThreadVariable<SystemTime?>("systemTime");

    public static SystemTime? Current => currentVariable.Value;

    public static IDisposable Override(DateTimeOffset asOf) => Override(new SystemTime.AsOf(asOf));
    public static IDisposable Override(SystemTime? systemTime)
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
        public DateTimeOffset DateTime { get; private set; }

        public AsOf(DateTimeOffset dateTime)
        {
            this.DateTime = dateTime;
        }

        public override string ToString() => $"AS OF {DateTime:u}";
    }



    public abstract class Interval : SystemTime
    {
        public JoinBehaviour JoinBehaviour; 
        public Interval(JoinBehaviour includeJoins)
        {
            this.JoinBehaviour = includeJoins;
        }
    }

    public class Between : Interval
    {
        public DateTimeOffset StartDateTime { get; private set; }
        public DateTimeOffset EndtDateTime { get; private set; }

        public Between(DateTimeOffset startDateTime, DateTimeOffset endDateTime, JoinBehaviour joinBehaviour) : base(joinBehaviour)
        {
            this.StartDateTime = startDateTime;
            this.EndtDateTime = endDateTime;
        }

        public override string ToString() => $"BETWEEN {StartDateTime:u} AND {EndtDateTime:u}";
    }

    public class ContainedIn : Interval
    {
        public DateTimeOffset StartDateTime { get; private set; }
        public DateTimeOffset EndtDateTime { get; private set; }

        public ContainedIn(DateTimeOffset startDateTime, DateTimeOffset endDateTime, JoinBehaviour joinBehaviour) : base(joinBehaviour)
        {
            this.StartDateTime = startDateTime;
            this.EndtDateTime = endDateTime;
        }

        public override string ToString() => $"CONTAINED IN ({StartDateTime:u}, {EndtDateTime:u})";
    }

    public class All : Interval
    {
        public All(JoinBehaviour joinBehaviour) : base(joinBehaviour)
        {
        }

        public override string ToString() => $"ALL";
    }
}

public enum JoinBehaviour
{
    Current,
    FirstCompatible,
    AllCompatible,
}

public static class SystemTimeExtensions
{
    public static NullableInterval<DateTime> SystemPeriod(this Entity entity)
    {
        throw new InvalidOperationException("Only for queries");
    }

    public static NullableInterval<DateTime> SystemPeriod<E, T>(this MListElement<E, T> mlistElement)
        where E : Entity
    {
        throw new InvalidOperationException("Only for queries");
    }
}
