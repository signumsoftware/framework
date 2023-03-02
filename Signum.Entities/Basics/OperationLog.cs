
namespace Signum.Entities.Basics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
public class OperationLogEntity : Entity
{
    public OperationLogEntity()
    {
        BindParent();
    }

    [ImplementedByAll]
    public Lite<IEntity>? Target { get; set; }

    [ImplementedByAll]
    public Lite<IEntity>? Origin { get; set; }

    public OperationSymbol Operation { get; set; }

    public Lite<IUserEntity> User { get; set; }

    [Format("G")]
    public DateTime Start { get; set; }

    [Format("G")]
    public DateTime? End { get; set; }

    static Expression<Func<OperationLogEntity, double?>> DurationExpression =
        log => (double?)(log.End - log.Start)!.Value.TotalMilliseconds;
    [ExpressionField("DurationExpression"), Unit("ms")]
    public double? Duration
    {
        get { return End == null ? null : DurationExpression.Evaluate(this); }
    }

    public Lite<ExceptionEntity>? Exception { get; set; }

    public override string ToString()
    {
        return "{0} {1} {2:d}".FormatWith(Operation, User, Start);
    }

    public void SetTarget(IEntity? target)
    {
        this.temporalTarget = target;
        this.Target = target == null || target.IsNew ? null : target.ToLite();
    }

    [Ignore]
    IEntity? temporalTarget;
    public IEntity? GetTemporalTarget()
    {
        return temporalTarget;
    }
}


