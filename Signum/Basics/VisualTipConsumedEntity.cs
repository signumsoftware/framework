namespace Signum.Basics;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class VisualTipConsumedEntity : Entity
{
    public VisualTipSymbol VisualTip { get; set; }

    public Lite<IUserEntity> User { get; set; }

    public DateTime ConsumedOn { get; set; }
}

[AutoInit]
public static class VisualTipConsumedOperation
{
    public static readonly DeleteSymbol<VisualTipConsumedEntity> Delete;
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class VisualTipSymbol : Symbol
{
    private VisualTipSymbol() { }

    public VisualTipSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class SearchVisualTip
{
    public static readonly VisualTipSymbol SearchHelp;
    public static readonly VisualTipSymbol GroupHelp;
    public static readonly VisualTipSymbol FilterHelp;
    public static readonly VisualTipSymbol ColumnHelp;
}

public enum VisualTipMessage
{
    Help
}
