using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Chart
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master), TicksColumn(false)]
    public class ChartColorEntity : Entity
    {
        [ImplementedByAll, UniqueIndex]
        Lite<Entity> related;
        [NotNullValidator]
        public Lite<Entity> Related
        {
            get { return related; }
            set { SetToStr(ref related, value); }
        }

        [NotNullable]
        ColorEntity color;
        //[NotNullValidator]
        public ColorEntity Color
        {
            get { return color; }
            set { SetToStr(ref color, value); }
        }

        public override string ToString()
        {
            if (related == null)
                return " -> {0}".FormatWith(color);


            return "{0} {1} -> {2}".FormatWith(related.GetType().NiceName(), related.Id, color);
        }
    }

    [Serializable]
    public class ChartPaletteModel : ModelEntity
    {
        TypeEntity type;
        [NotNullValidator]
        public TypeEntity Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable]
        MList<ChartColorEntity> colors = new MList<ChartColorEntity>();
        public MList<ChartColorEntity> Colors
        {
            get { return colors; }
            set { Set(ref colors, value); }
        }

        public override string ToString()
        {
            return ChartMessage.ColorsFor0.NiceToString().FormatWith(type.CleanName);
        }
    }
}
