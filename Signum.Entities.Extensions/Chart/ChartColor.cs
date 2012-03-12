using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

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
        [NotNullValidator]
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
}
