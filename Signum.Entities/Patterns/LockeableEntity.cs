using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Patterns
{
    [Serializable]
    public class LockeableEntity : Entity
    {
        bool locked;
        public bool Locked
        {
            get { return locked; }
            set { base.Set(ref locked, value, "Locked"); }
        }

        protected override bool Set<T>(ref T variable, T value, string propertyName)
        {
            if (this.locked)
                throw new ApplicationException("Modification not allowed: the object is locked");

            return base.Set<T>(ref variable, value, propertyName);
        }

    }
}
