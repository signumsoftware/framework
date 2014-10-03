using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Chart
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public class ChartColorDN : Entity
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
        ColorDN color;
        //[NotNullValidator]
        public ColorDN Color
        {
            get { return color; }
            set { SetToStr(ref color, value); }
        }

        public override string ToString()
        {
            if (related == null)
                return " -> {0}".Formato(color);


            return "{0} {1} -> {2}".Formato(related.GetType().NiceName(), related.Id, color);
        }
    }

    [Serializable]
    public class ChartPaletteModel : ModelEntity
    {
        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable]
        MList<ChartColorDN> colors = new MList<ChartColorDN>();
        public MList<ChartColorDN> Colors
        {
            get { return colors; }
            set { Set(ref colors, value); }
        }

        public override string ToString()
        {
            return ChartMessage.ColorsFor0.NiceToString().Formato(type.CleanName);
        }
    }
}
