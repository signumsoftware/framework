using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Properties;
using System.Linq.Expressions;

namespace Signum.Entities.Patterns
{
    [Serializable]
    public abstract class LockeableEntity : Entity
    {
        bool locked;
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (UnsafeSet(ref locked, value, () => Locked))
                    ItemLockedChanged(Locked);
            }
        }

        protected bool UnsafeSet<T>(ref T variable, T value, Expression<Func<T>> property)
        { 
            return base.Set<T>(ref variable, value, property);
        }

        protected virtual void ItemLockedChanged(bool locked)
        {
        }

        protected override bool Set<T>(ref T variable, T value, Expression<Func<T>> property)
        {
            if (this.locked)
                throw new ApplicationException(Resources.LockedModificationException);

            return base.Set<T>(ref variable, value, property);
        }

        [HiddenProperty]
        public override string ToStr
        {
            get { return toStr; }
            protected set { UnsafeSet(ref toStr, value, ()=>ToStr); }
        }
    }
}
