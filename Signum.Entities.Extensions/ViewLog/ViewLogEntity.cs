using Signum.Entities.Basics;

namespace Signum.Entities.ViewLog;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ViewLogEntity : Entity
{
    public ViewLogEntity()
    {
        BindParent();
    }

    [ImplementedByAll]
    public Lite<Entity> Target { get; set; }
    
    public Lite<IUserEntity> User { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string ViewAction { get; set; }

    [Format("G")]
    public DateTime StartDate { get; private set; } = Clock.Now;

    [Format("G")]
    public DateTime EndDate { get; set; }

    [BindParent]
    public BigStringEmbedded Data { get; set; } = new BigStringEmbedded();

    [AutoExpressionField, Unit("ms")]
    public double Duration => As.Expression(() => (EndDate - StartDate).TotalMilliseconds);
}

public enum ViewLogMessage
{
    ViewLogMyLast
}
