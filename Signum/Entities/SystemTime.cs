using Signum.Utilities.DataStructures;

namespace Signum.Entities;


public abstract class SystemTime
{
    [AvoidEagerEvaluation]
    public static T OverrideInExpression<T>(SystemTime systemTime, T value)
    {
        throw new InvalidOperationException("Only for Queries");
    }

    static Variable<SystemTime?> currentVariable = Statics.ThreadVariable<SystemTime?>("systemTime");

    public static SystemTime? Current => currentVariable.Value;

    public static IDisposable Override(DateTime asOf) => Override(new SystemTime.AsOf(asOf));
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
        public DateTime DateTime { get; private set; }

        [NewCanBeConstant]
        public AsOf(DateTime dateTime)
        {
            this.DateTime = dateTime;
        }

        public override string ToString() => $"AS OF {DateTime:u}";
    }

    internal class AsOfExpression : SystemTime
    {
        public Expression Expression { get; private set; }

        [NewCanBeConstant]
        public AsOfExpression(Expression dateTime)
        {
            this.Expression = dateTime;
        }

        public override string ToString() => $"EVERY {Expression:u}";
    }

    public abstract class Interval : SystemTime
    {
        public SystemTimeJoinMode JoinMode;
        [NewCanBeConstant]
        public Interval(SystemTimeJoinMode joinMode)
        {
            this.JoinMode = joinMode;
        }
    }

    public class Between : Interval
    {
        public DateTime StartDateTime { get; private set; }
        public DateTime EndtDateTime { get; private set; }
        [NewCanBeConstant]
        public Between(DateTime startDateTime, DateTime endDateTime, SystemTimeJoinMode joinMode) : base(joinMode)
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
        [NewCanBeConstant]
        public ContainedIn(DateTime startDateTime, DateTime endDateTime, SystemTimeJoinMode joinMode) : base(joinMode)
        {
            this.StartDateTime = startDateTime;
            this.EndtDateTime = endDateTime;
        }

        public override string ToString() => $"CONTAINED IN ({StartDateTime:u}, {EndtDateTime:u})";
    }

    public class All : Interval
    {
        [NewCanBeConstant]
        public All(SystemTimeJoinMode joinMode) : base(joinMode)
        {
        }

        public override string ToString() => $"ALL";
    }
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
