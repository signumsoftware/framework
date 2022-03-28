using Signum.Entities.Basics;

namespace Signum.Entities.Chart;

[EntityKind(EntityKind.System, EntityData.Master), TicksColumn(false)]
public class ChartColorEntity : Entity
{
    [ImplementedByAll, UniqueIndex]
    public Lite<Entity> Related { get; set; }

    [StringLengthValidator(Max = 100), NotNullValidator(DisabledInModelBinder = true)]
    public string Color { get; set; }

    public override string ToString()
    {
        if (Related == null)
            return " -> {0}".FormatWith(Color);


        return "{0} {1} -> {2}".FormatWith(Related.GetType().NiceName(), Related.Id, Color);
    }
}

public class ChartPaletteModel : ModelEntity
{   
    public string TypeName { get; set; }
    
    public MList<ChartColorEntity> Colors { get; set; } = new MList<ChartColorEntity>();

    public override string ToString()
    {
        var type = TypeEntity.TryGetType(TypeName);

        return ChartMessage.ColorsFor0.NiceToString().FormatWith(type?.NiceName() ?? TypeName);
    }
}
