using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [Serializable]
    public abstract class Entity : IdentifiableEntity
    {
        internal long ticks = 0;
        [HiddenProperty]
        public long Ticks
        {
            get { return ticks; }
            internal set { ticks = value; }
        }
    }
}
