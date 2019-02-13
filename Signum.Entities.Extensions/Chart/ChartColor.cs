using System;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Chart
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master), TicksColumn(false)]
    public class ChartColorEntity : Entity
    {
        [ImplementedByAll, UniqueIndex]
        public Lite<Entity> Related { get; set; }

        [ForceNotNullable]
        public ColorEmbedded? Color { get; set; }

        public override string ToString()
        {
            if (Related == null)
                return " -> {0}".FormatWith(Color);


            return "{0} {1} -> {2}".FormatWith(Related.GetType().NiceName(), Related.Id, Color);
        }
    }

    [Serializable]
    public class ChartPaletteModel : ModelEntity
    {
        
        public TypeEntity Type { get; set; }

        
        public MList<ChartColorEntity> Colors { get; set; } = new MList<ChartColorEntity>();

        public override string ToString()
        {
            return ChartMessage.ColorsFor0.NiceToString().FormatWith(Type.CleanName);
        }
    }
}
