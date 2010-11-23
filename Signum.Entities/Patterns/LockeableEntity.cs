using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Properties;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Utilities;

namespace Signum.Entities.Patterns
{
    [Serializable]
    public abstract class LockableEntity : Entity
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

        protected bool UnsafeSet<T>(ref T field, T value, Expression<Func<T>> property)
        { 
            return base.Set<T>(ref field, value, property);
        }

        protected virtual void ItemLockedChanged(bool locked)
        {
        }

        protected override bool Set<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (this.locked)
                throw new InvalidOperationException("Attempt to modify locked entity {0}".Formato(this.ToString()));
            
            return base.Set<T>(ref field, value, property);
        }

        protected internal override void PreSaving(ref bool graphModified)
        {
            UnsafeSet(ref toStr, ToString(), () => ToStr);
        }
    }
}
