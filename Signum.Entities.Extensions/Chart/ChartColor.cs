using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class ChartColorDN : IdentifiableEntity
    {
        [ImplementedByAll, UniqueIndex]
        Lite<IdentifiableEntity> related;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Related
        {
            get { return related; }
            set { SetToStr(ref related, value, () => Related); }
        }

        [NotNullable]
        ColorDN color;
        //[NotNullValidator]
        public ColorDN Color
        {
            get { return color; }
            set { SetToStr(ref color, value, () => Color); }
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
            set { Set(ref type, value, () => Type); }
        }

        [NotNullable]
        MList<ChartColorDN> colors = new MList<ChartColorDN>();
        public MList<ChartColorDN> Colors
        {
            get { return colors; }
            set { Set(ref colors, value, () => Colors); }
        }

        public override string ToString()
        {
            return Resources.ColorsFor0.Formato(type.CleanName);
        }
    }
}
